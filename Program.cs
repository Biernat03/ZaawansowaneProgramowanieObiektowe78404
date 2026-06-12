using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BikeRental
{
    public interface IWypozyczalny
    {
        bool CzyDostepny();
        bool Wypozycz(string klient, int liczbaDni);
        bool Zwroc();
        void WyswietlInformacje();
    }

    public abstract class Rower : IWypozyczalny
    {
        private static int kolejnyId = 1;

        public int Id { get; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public int RokProdukcji { get; protected set; }
        public decimal CenaZaDzien { get; set; }
        public bool CzyWypozyczony { get; protected set; }
        public string? AktualnyKlient { get; protected set; }
        public DateTime? DataZwrotu { get; protected set; }

        protected Rower(string marka, string model, int rokProdukcji, decimal cenaZaDzien)
        {
            Id = kolejnyId++;
            Marka = marka;
            Model = model;
            RokProdukcji = rokProdukcji;
            CenaZaDzien = cenaZaDzien;
        }

        public bool CzyDostepny()
        {
            return !CzyWypozyczony;
        }

        public bool Wypozycz(string klient, int liczbaDni)
        {
            if (CzyWypozyczony || string.IsNullOrWhiteSpace(klient) || liczbaDni <= 0)
                return false;

            CzyWypozyczony = true;
            AktualnyKlient = klient;
            DataZwrotu = DateTime.Today.AddDays(liczbaDni);
            return true;
        }

        public bool Zwroc()
        {
            if (!CzyWypozyczony)
                return false;

            CzyWypozyczony = false;
            AktualnyKlient = null;
            DataZwrotu = null;
            return true;
        }

        public virtual void WyswietlInformacje()
        {
            Console.WriteLine($"[{Id}] {Marka} {Model}");
            Console.WriteLine($"    Typ            : {PobierzTypRoweru()}");
            Console.WriteLine($"    Rok produkcji  : {RokProdukcji}");
            Console.WriteLine($"    Cena / dzień   : {CenaZaDzien:C}");
            Console.WriteLine($"    Status         : {(CzyWypozyczony ? "Wypożyczony" : "Dostępny")}");

            if (CzyWypozyczony)
            {
                Console.WriteLine($"    Klient         : {AktualnyKlient}");
                Console.WriteLine($"    Termin zwrotu  : {DataZwrotu:yyyy-MM-dd}");
            }
        }

        public abstract string PobierzTypRoweru();
    }

    public class RowerMiejski : Rower
    {
        public int LiczbaBiegow { get; set; }

        public RowerMiejski(string marka, string model, int rokProdukcji, decimal cenaZaDzien, int liczbaBiegow)
            : base(marka, model, rokProdukcji, cenaZaDzien)
        {
            LiczbaBiegow = liczbaBiegow;
        }

        public override void WyswietlInformacje()
        {
            base.WyswietlInformacje();
            Console.WriteLine($"    Biegi          : {LiczbaBiegow}");
        }

        public override string PobierzTypRoweru()
        {
            return "Miejski";
        }

        public static RowerMiejski operator ++(RowerMiejski rower)
        {
            rower.CenaZaDzien *= 1.10m;
            return rower;
        }
    }

    public class RowerGorski : Rower
    {
        public int SkokAmortyzacji { get; set; }

        public RowerGorski(string marka, string model, int rokProdukcji, decimal cenaZaDzien, int skokAmortyzacji)
            : base(marka, model, rokProdukcji, cenaZaDzien)
        {
            SkokAmortyzacji = skokAmortyzacji;
        }

        public override void WyswietlInformacje()
        {
            base.WyswietlInformacje();
            Console.WriteLine($"    Amortyzacja    : {SkokAmortyzacji} mm");
        }

        public override string PobierzTypRoweru()
        {
            return "Górski";
        }
    }

    public class Wypozyczalnia<T> where T : Rower
    {
        private readonly List<T> rowery = new();

        public delegate void RowerDodanyEventHandler(object sender, T rower);
        public event RowerDodanyEventHandler? RowerDodany;

        public T? this[int id] => rowery.FirstOrDefault(r => r.Id == id);

        public void DodajRower(T rower)
        {
            rowery.Add(rower);
            RowerDodany?.Invoke(this, rower);
        }

        public IEnumerable<T> PobierzWszystkie()
        {
            return rowery;
        }

        public IEnumerable<T> PobierzDostepne()
        {
            return rowery.Where(r => r.CzyDostepny());
        }

        public IEnumerable<T> PobierzWypozyczone()
        {
            return rowery.Where(r => !r.CzyDostepny());
        }
    }

    public static class NarzedziaWypozyczalni
    {
        public static void WyswietlSzczegolyObiektu(object obiekt)
        {
            UI.Naglowek($"Szczegóły roweru: {obiekt.GetType().Name}");

            var properties = obiekt.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.Name)
                .ToArray();

            if (properties.Length == 0)
            {
                UI.Info("Obiekt nie posiada publicznych właściwości.");
                return;
            }

            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                var value = property.GetValue(obiekt);
                Console.WriteLine($"{property.Name,-18} : {value ?? "brak"}");
            }
        }
    }

    public static class Logi
    {
        public static void Zaloguj(string komunikat)
        {
            var kolor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {komunikat}");
            Console.ForegroundColor = kolor;
        }
    }

    public static class UI
    {
        public static void Naglowek(string tekst)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($" {tekst}");
            Console.WriteLine(new string('═', 60));
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void Info(string tekst)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[INFO] {tekst}");
            Console.ResetColor();
        }

        public static void Sukces(string tekst)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK]   {tekst}");
            Console.ResetColor();
        }

        public static void Blad(string tekst)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERR]  {tekst}");
            Console.ResetColor();
        }

        public static void Linia()
        {
            Console.WriteLine(new string('-', 60));
        }

        public static void Czekaj()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Naciśnij dowolny klawisz, aby kontynuować...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        public static void PokazMenu()
        {
            Naglowek("System wypożyczalni rowerów");
            Console.WriteLine("1. Pokaż wszystkie rowery");
            Console.WriteLine("2. Pokaż dostępne rowery");
            Console.WriteLine("3. Wypożycz rower");
            Console.WriteLine("4. Zwróć rower");
            Console.WriteLine("5. Podnieś cenę dzienną roweru 1");
            Console.WriteLine("6. Pokaż szczegóły roweru");
            Console.WriteLine("7. Uruchom kontrolę nocną w tle");
            Console.WriteLine("0. Wyjście");
            Console.WriteLine();
            Console.Write("Wybierz opcję: ");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Bike Rental";

            var wypozyczalnia = new Wypozyczalnia<Rower>();
            wypozyczalnia.RowerDodany += (sender, rower) =>
            {
                Logi.Zaloguj($"Dodano rower #{rower.Id}: {rower.Marka} {rower.Model} ({rower.PobierzTypRoweru()})");
            };

            var rower1 = new RowerMiejski("Kross", "Urban 3.0", 2022, 45m, 7);
            var rower2 = new RowerGorski("Trek", "Marlin 7", 2023, 70m, 120);
            var rower3 = new RowerMiejski("Gazelle", "CityGo", 2021, 40m, 5);

            wypozyczalnia.DodajRower(rower1);
            wypozyczalnia.DodajRower(rower2);
            wypozyczalnia.DodajRower(rower3);

            rower2.Wypozycz("Jan Kowalski", 3);

            UI.Naglowek("System wypożyczalni rowerów");
            UI.Info($"Rower #{rower2.Id} jest już wypożyczony do {rower2.DataZwrotu:yyyy-MM-dd}.");
            UI.Czekaj();

            await UruchomMenu(wypozyczalnia);
        }

        static async Task UruchomMenu(Wypozyczalnia<Rower> wypozyczalnia)
        {
            while (true)
            {
                UI.PokazMenu();
                var wybor = Console.ReadLine();

                switch (wybor)
                {
                    case "1":
                        PokazWszystkie(wypozyczalnia);
                        break;
                    case "2":
                        PokazDostepne(wypozyczalnia);
                        break;
                    case "3":
                        WypozyczRower(wypozyczalnia);
                        break;
                    case "4":
                        ZwrocRower(wypozyczalnia);
                        break;
                    case "5":
                        ZwiekszCene(wypozyczalnia);
                        break;
                    case "6":
                        PokazSzczegolyRoweru(wypozyczalnia);
                        break;
                    case "7":
                        _ = KontrolaNocnaAsync();
                        UI.Naglowek("Kontrola nocna");
                        UI.Sukces("Kontrola została uruchomiona w tle.");
                        UI.Czekaj();
                        break;
                    case "0":
                        UI.Naglowek("Zamykanie programu");
                        UI.Info("Do zobaczenia.");
                        return;
                    default:
                        UI.Naglowek("Błąd");
                        UI.Blad("Nieznana opcja.");
                        UI.Czekaj();
                        break;
                }
            }
        }

        static void PokazWszystkie(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Wszystkie rowery");

            var rowery = wypozyczalnia.PobierzWszystkie().ToList();

            if (!rowery.Any())
            {
                UI.Info("Brak rowerów w systemie.");
                UI.Czekaj();
                return;
            }

            foreach (var rower in rowery)
            {
                rower.WyswietlInformacje();
                UI.Linia();
            }

            UI.Czekaj();
        }

        static void PokazDostepne(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Dostępne rowery");

            var rowery = wypozyczalnia.PobierzDostepne().ToList();

            if (!rowery.Any())
            {
                UI.Info("Brak dostępnych rowerów.");
                UI.Czekaj();
                return;
            }

            foreach (var rower in rowery)
            {
                rower.WyswietlInformacje();
                UI.Linia();
            }

            UI.Czekaj();
        }

        static void WypozyczRower(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Wypożyczenie roweru");

            Console.Write("Podaj Id roweru: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                UI.Blad("Nieprawidłowe Id.");
                UI.Czekaj();
                return;
            }

            var rower = wypozyczalnia[id];
            if (rower == null)
            {
                UI.Blad("Nie znaleziono roweru.");
                UI.Czekaj();
                return;
            }

            Console.Write("Podaj imię i nazwisko klienta: ");
            var klient = Console.ReadLine() ?? "";

            Console.Write("Podaj liczbę dni wypożyczenia: ");
            if (!int.TryParse(Console.ReadLine(), out var liczbaDni))
            {
                UI.Blad("Nieprawidłowa liczba dni.");
                UI.Czekaj();
                return;
            }

            if (rower.Wypozycz(klient, liczbaDni))
            {
                var koszt = rower.CenaZaDzien * liczbaDni;
                UI.Sukces($"Rower #{rower.Id} został wypożyczony.");
                Console.WriteLine($"Klient          : {klient}");
                Console.WriteLine($"Liczba dni      : {liczbaDni}");
                Console.WriteLine($"Termin zwrotu   : {rower.DataZwrotu:yyyy-MM-dd}");
                Console.WriteLine($"Koszt łączny    : {koszt:C}");
            }
            else
            {
                UI.Blad("Nie udało się wypożyczyć roweru.");
            }

            UI.Czekaj();
        }

        static void ZwrocRower(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Zwrot roweru");

            Console.Write("Podaj Id roweru: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                UI.Blad("Nieprawidłowe Id.");
                UI.Czekaj();
                return;
            }

            var rower = wypozyczalnia[id];
            if (rower == null)
            {
                UI.Blad("Nie znaleziono roweru.");
                UI.Czekaj();
                return;
            }

            if (rower.Zwroc())
            {
                UI.Sukces($"Rower #{rower.Id} został zwrócony.");
            }
            else
            {
                UI.Blad("Ten rower nie jest aktualnie wypożyczony.");
            }

            UI.Czekaj();
        }

        static void ZwiekszCene(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Zmiana ceny dziennej");

            if (wypozyczalnia[1] is RowerMiejski rowerMiejski)
            {
                var staraCena = rowerMiejski.CenaZaDzien;
                rowerMiejski++;

                UI.Sukces($"Zmieniono cenę roweru #{rowerMiejski.Id}.");
                Console.WriteLine($"Poprzednia cena : {staraCena:C}");
                Console.WriteLine($"Nowa cena       : {rowerMiejski.CenaZaDzien:C}");
            }
            else
            {
                UI.Blad("Nie znaleziono roweru miejskiego o Id = 1.");
            }

            UI.Czekaj();
        }

        static void PokazSzczegolyRoweru(Wypozyczalnia<Rower> wypozyczalnia)
        {
            UI.Naglowek("Szczegóły roweru");
            Console.Write("Podaj Id roweru: ");

            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                UI.Blad("Nieprawidłowe Id.");
                UI.Czekaj();
                return;
            }

            var rower = wypozyczalnia[id];
            if (rower == null)
            {
                UI.Blad("Nie znaleziono roweru.");
                UI.Czekaj();
                return;
            }

            NarzedziaWypozyczalni.WyswietlSzczegolyObiektu(rower);
            UI.Czekaj();
        }

        static async Task KontrolaNocnaAsync()
        {
            Logi.Zaloguj("Start nocnej kontroli dostępności rowerów.");
            await Task.Delay(3000);
            Logi.Zaloguj("Nocna kontrola zakończona.");
        }
    }
}