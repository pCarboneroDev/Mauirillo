namespace ENT
{
    public class Sala
    {
        #region Atributos
        private int idSala;
        private string nombre;
        private List<Jugador> jugadores;
        #endregion

        #region Propiedades
        public int IdSala { get { return idSala; } set { idSala = value; } }
        public string Nombre { get { return nombre; } set { nombre = value; } }
        public List<Jugador> Jugadores { get { return jugadores; } set { jugadores = value; } }
        #endregion

        #region Constructor
        public Sala() { }

        public Sala(string nombre, List<Jugador> jugadores)
        {
            this.nombre = nombre;
            this.jugadores = jugadores;
        }
        public Sala(int idSala, string nombre, List<Jugador> jugadores)
        {
            this.idSala = idSala;
            this.nombre = nombre;
            this.jugadores = jugadores;
        }
        #endregion
    }
}
