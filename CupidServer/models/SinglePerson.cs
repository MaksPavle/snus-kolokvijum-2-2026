namespace CupidServer.models
{
    // Model koji predstavlja osobu prijavljenu za traženje partnera.
    public class SinglePerson
    {
        public string Username { get; set; } = "";
        public string City { get; set; } = "";
        public int Age { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string ConnectionId { get; set; } = "";

        // Dok osoba ne potvrdi prethodno pismo, ne sme da primi novo.
        public bool WaitingConfirmation { get; set; } = false;

        // Lista korisnika koje je ova osoba blokirala.
        public List<string> BlockedUsers { get; set; } = new();
    }
}