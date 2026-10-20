using System.Linq;

namespace Atletika
{
    public class Versenyzo
    {
        public int Rajtszam { get; set; }
        public string Nev { get; set; } = "";
        public string Versenyszam { get; set; } = "";
        public string Nem { get; set; } = "";
        public int Eletkor { get; set; }
        public bool OR { get; set; }
        public bool VR { get; set; }
        public bool ER { get; set; }

        public Versenyzo(int rajtszam, string nev, string versenyszam, string nem, int eletkor, bool or, bool vr, bool er)
        {
            Rajtszam = rajtszam;
            Nev = nev;
            Versenyszam = versenyszam;
            Nem = nem;
            Eletkor = eletkor;
            OR = or;
            VR = vr;
            ER = er;
        }

        public Versenyzo(string sor)
        {
            string[] rawData = sor.Split(';').Select(m => m.Trim()).ToArray();

            Rajtszam = int.Parse(rawData[0]);
            Nev = rawData[1];
            Versenyszam = rawData[2];
            Nem = rawData[3];
            Eletkor = int.Parse(rawData[4]);
            OR = rawData[5] == "1";
            VR = rawData[6] == "1";
            ER = rawData[7] == "1";
        }
    }
}
