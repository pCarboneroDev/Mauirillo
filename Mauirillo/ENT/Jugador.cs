namespace ENT
{
    public class Jugador
    {
        #region Atributos

        private int idJugador;
        private string nombre;
        private bool tipo;
        private int puntuacion;

        #endregion

        #region Propiedades
        public int IdJugador { get { return idJugador; } set { idJugador = value; } }
        public string Nombre { get { return nombre; } set { nombre = value; } }
        public bool Tipo { get { return tipo; } set { tipo = value; } }
        public int Puntuacion { get { return puntuacion; } set { puntuacion = value; } }
        #endregion

        #region Constructor
        public Jugador() { }
        
        public Jugador(string nombre)
        {
            this.nombre = nombre;
        }

        public Jugador(int idJugador, string nombre, bool tipo, int puntuacion)
        {
            this.idJugador = idJugador;
            this.nombre = nombre;
            this.tipo = tipo;
            this.puntuacion = puntuacion;
        }

        #endregion
    }
}
