using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("=== Haotični kupidon - osoba ===");

// Unos i validacija podataka osobe.
string username = ReadRequiredText("Unesite username: ");
string city = ReadRequiredText("Unesite grad: ");
int age = ReadAge("Unesite godine: ");
string phoneNumber = ReadPhoneNumber("Unesite broj telefona: ");

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7196/cupidHub")
    .WithAutomaticReconnect()
    .Build();

bool waitingConfirmation = false;
// Callback koji se izvršava kada server pošalje ljubavno pismo ovom klijentu.
connection.On<string, string, int, string, string>(
    "ReceiveLoveLetter",
    async (senderUsername, senderCity, senderAge, senderPhone, message) =>
    {
        waitingConfirmation = true;

        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("Primili ste ljubavno pismo!");
        Console.WriteLine($"Od: {senderUsername}");
        Console.WriteLine($"Grad: {senderCity}");
        Console.WriteLine($"Godine: {senderAge}");
        Console.WriteLine($"Poruka: {message}");

        if (senderPhone != "Sakriven broj telefona")
        {
            Console.WriteLine($"Broj telefona: {senderPhone}");
        }
        else
        {
            Console.WriteLine("Broj telefona nije prikazan.");
        }

        Console.WriteLine("Unesite /ok da potvrdite prijem pisma.");
        Console.WriteLine("Možete uneti i /block username da blokirate korisnika.");
        Console.WriteLine("=================================");
        Console.WriteLine();

        await Task.CompletedTask;
    });

try
{
    await connection.StartAsync();
    var registrationSuccess = await connection.InvokeAsync<bool>(
        "InitSinglePerson",
        username,
        city,
        age,
        phoneNumber
    );

    if (!registrationSuccess)
    {
        Console.WriteLine("Korisnik sa tim username-om već postoji. Pokrenite klijenta ponovo i unesite drugi username.");
        Console.ReadLine();
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Uspešno ste prijavljeni za traženje partnera.");
    Console.WriteLine("Kupidon šalje pisma na svakih 1 minut.");
    Console.WriteLine("Komande:");
    Console.WriteLine("/ok - potvrda prijema pisma");
    Console.WriteLine("/block username - blokiranje korisnika");
    Console.WriteLine("/exit - izlaz");
    Console.WriteLine();

    // Glavna petlja za unos komandi korisnika.
    while (true)
    {
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        if (input.Equals("/ok", StringComparison.OrdinalIgnoreCase))
        {
            if (!waitingConfirmation)
            {
                Console.WriteLine("Trenutno nemate pismo za potvrdu.");
                continue;
            }

            await connection.InvokeAsync("ConfirmReceived", username);
            waitingConfirmation = false;
            Console.WriteLine("Potvrdili ste prijem pisma.");
            continue;
        }

        if (input.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
        {
            var blockedUsername = input.Substring("/block ".Length).Trim();

            if (string.IsNullOrWhiteSpace(blockedUsername))
            {
                Console.WriteLine("Morate uneti username koji želite da blokirate.");
                continue;
            }

            await connection.InvokeAsync("BlockUser", username, blockedUsername);
            Console.WriteLine($"Blokirali ste korisnika: {blockedUsername}");
            continue;
        }

        Console.WriteLine("Nepoznata komanda. Koristite /ok, /block username ili /exit.");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Greška: " + ex.Message);
}
finally
{
    await connection.StopAsync();
}

static string ReadRequiredText(string message)
{
    while (true)
    {
        Console.Write(message);
        string? input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            return input.Trim();
        }

        Console.WriteLine("Polje ne sme biti prazno.");
    }
}

static int ReadAge(string message)
{
    while (true)
    {
        Console.Write(message);
        string? input = Console.ReadLine();

        if (!int.TryParse(input, out int number))
        {
            Console.WriteLine("Morate uneti godine kao broj, a ne karaktere.");
            continue;
        }

        if (number < 16)
        {
            Console.WriteLine("Osoba mora imati najmanje 16 godina.");
            continue;
        }

        if (number > 120)
        {
            Console.WriteLine("Unete godine nisu realne.");
            continue;
        }

        return number;
    }
}

static string ReadPhoneNumber(string message)
{
    while (true)
    {
        Console.Write(message);
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Broj telefona ne sme biti prazan.");
            continue;
        }

        input = input.Trim();

        if (!input.All(char.IsDigit))
        {
            Console.WriteLine("Broj telefona sme da sadrži samo cifre.");
            continue;
        }

        if (input.Length < 6)
        {
            Console.WriteLine("Broj telefona je prekratak.");
            continue;
        }

        return input;
    }
}