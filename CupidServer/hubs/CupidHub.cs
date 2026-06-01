using CupidServer.services;
using Microsoft.AspNetCore.SignalR;

namespace CupidServer.hubs
{
    // SignalR Hub predstavlja komunikacionu tačku između konzolnih klijenata i servera.
    public class CupidHub : Hub
    {
        private readonly CupidService _cupidService;

        public CupidHub(CupidService cupidService)
        {
            _cupidService = cupidService;
        }

        // Metoda preko koje se korisnik prijavljuje za traženje partnera.
        public Task<bool> InitSinglePerson(string username, string city, int age, string phoneNumber)
        {
            var success = _cupidService.AddPerson(username, city, age, phoneNumber, Context.ConnectionId);

            if (success)
            {
                Console.WriteLine($"[SERVER] Registered: {username}, {city}, {age}");
            }
            else
            {
                Console.WriteLine($"[SERVER] Registration rejected. Username already exists: {username}");
            }

            return Task.FromResult(success);
        }

        // Korisnik potvrđuje da je primio prethodno pismo, nakon čega može primiti novo.
        public Task ConfirmReceived(string username)
        {
            _cupidService.ConfirmReceived(username);
            Console.WriteLine($"[SERVER] {username} confirmed received letter.");
            return Task.CompletedTask;
        }

        // Korisnik blokira drugog korisnika kako ubuduće ne bi primao pisma od njega.
        public Task BlockUser(string username, string blockedUsername)
        {
            _cupidService.BlockUser(username, blockedUsername);
            Console.WriteLine($"[SERVER] {username} blocked {blockedUsername}");
            return Task.CompletedTask;
        }

        // Kada se klijent diskonektuje, uklanja se iz liste prijavljenih osoba.
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _cupidService.RemoveByConnectionId(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}