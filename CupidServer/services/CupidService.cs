using CupidServer.hubs;
using CupidServer.models;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;

namespace CupidServer.services
{
    // Background servis koji čuva prijavljene osobe i periodično šalje ljubavna pisma.
    public class CupidService : BackgroundService
    {
        private readonly IHubContext<CupidHub> _hubContext;
        private readonly List<SinglePerson> _persons = new();   // Lista trenutno prijavljenih osoba. Čuva se u memoriji servera.
        private readonly object _lock = new();  // Lock se koristi zbog bezbednog pristupa listi iz više paralelnih poziva.

        private readonly string[] _messages =
        {
            "Radujem se našem susretu!",
            "Želim da se upoznamo.",
            "Nisam zainteresovan/a za upoznavanje."
        };

        public CupidService(IHubContext<CupidHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // Dodaje novu osobu ukoliko username već nije zauzet.
        public bool AddPerson(string username, string city, int age, string phoneNumber, string connectionId)
        {
            lock (_lock)
            {
                var usernameExists = _persons.Any(p =>
                    p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (usernameExists)
                {
                    return false;
                }

                _persons.Add(new SinglePerson
                {
                    Username = username,
                    City = city,
                    Age = age,
                    PhoneNumber = phoneNumber,
                    ConnectionId = connectionId
                });

                return true;
            }
        }

        // Skida oznaku čekanja potvrde, čime korisnik ponovo može da primi pismo.
        public void ConfirmReceived(string username)
        {
            lock (_lock)
            {
                var person = _persons.FirstOrDefault(p =>
                    p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (person != null)
                {
                    person.WaitingConfirmation = false;
                }
            }
        }

        // Dodaje izabranog korisnika u listu blokiranih pošiljalaca.
        public void BlockUser(string username, string blockedUsername)
        {
            lock (_lock)
            {
                var person = _persons.FirstOrDefault(p =>
                    p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (person != null && !person.BlockedUsers.Contains(blockedUsername, StringComparer.OrdinalIgnoreCase))
                {
                    person.BlockedUsers.Add(blockedUsername);
                }
            }
        }

        public void RemoveByConnectionId(string connectionId)
        {
            lock (_lock)
            {
                var person = _persons.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (person != null)
                {
                    _persons.Remove(person);
                    Console.WriteLine($"[SERVER] Disconnected: {person.Username}");
                }
            }
        }

        // Glavna petlja background servisa. Na svakih 1 minut pokreće slanje pisama.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

                await SendLoveLetters();
            }
        }

        
        private async Task SendLoveLetters()
        {
            List<(SinglePerson receiver, SinglePerson sender, string message)> letters = new();

            lock (_lock)
            {
                foreach (var receiver in _persons)
                {
                    if (receiver.WaitingConfirmation)
                    {
                        continue;
                    }
                    // Pošiljalac ne sme biti ista osoba, niti korisnik koji je blokiran.
                    var possibleSenders = _persons
                        .Where(p => !p.Username.Equals(receiver.Username, StringComparison.OrdinalIgnoreCase))
                        .Where(p => !receiver.BlockedUsers.Contains(p.Username, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (!possibleSenders.Any())
                    {
                        continue;
                    }

                    var bestSender = possibleSenders
                        .OrderByDescending(sender => CalculateScore(receiver, sender))
                        .First();

                    var message = _messages[GetSecureRandomNumber(0, _messages.Length)];

                    // Osoba ne može dobiti novo pismo dok ne potvrdi prethodno.
                    receiver.WaitingConfirmation = true;
                    letters.Add((receiver, bestSender, message));
                }
            }

            foreach (var letter in letters)
            {
                var showPhone = letter.message != "Nisam zainteresovan/a za upoznavanje.";

                await _hubContext.Clients.Client(letter.receiver.ConnectionId).SendAsync(
                    "ReceiveLoveLetter",
                    letter.sender.Username,
                    letter.sender.City,
                    letter.sender.Age,
                    showPhone ? letter.sender.PhoneNumber : "Sakriven broj telefona",
                    letter.message
                );
            }
        }
        // Računa score prema pravilima: ista lokacija, slične godine i nasumični faktor.
        private int CalculateScore(SinglePerson receiver, SinglePerson sender)
        {
            int score = 0;

            if (receiver.City.Equals(sender.City, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
            }

            if (Math.Abs(receiver.Age - sender.Age) <= 2)
            {
                score += 20;
            }

            score += GetSecureRandomNumber(0, 101);

            return score;
        }

        // Generiše nasumičan broj pomoću RNGCryptoServiceProvider.
        private int GetSecureRandomNumber(int minValue, int maxValue)
        {
            using var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);

            var value = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return minValue + value % (maxValue - minValue);
        }
    }
}