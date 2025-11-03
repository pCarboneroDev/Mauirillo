using ENT;

namespace DTO
{
    public class Ronda
    {
        private List<Jugador> listaJugadores;
        private Jugador pintor;
        private string palabra;
        private int tiempo;

        public List<Jugador> ListaJugadores { get { return listaJugadores; } set { listaJugadores = value; } }
        public Jugador Pintor { get { return pintor; } set { pintor = value; } }
        public string Palabra { get { return palabra; } set { palabra = value; } }
        public int Tiempo { get { return tiempo; } set { tiempo = value; } }

        public Ronda() { }

        public Ronda(List<Jugador> listaJugadores, string palabra, Jugador pintor)
        {
            this.listaJugadores = listaJugadores;
            this.palabra = palabra;
            tiempo = 60;
            Random random = new Random();
            this.pintor = pintor;
        }
    }
}
