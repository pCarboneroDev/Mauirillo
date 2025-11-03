using ENT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.Model
{
    public class DatosConexionMdl
    {
        #region atributos
        private Jugador jugador;
        private string idSala;
        #endregion

        #region Propiedades
        public Jugador Jugador { get { return jugador; } set { jugador = value; } }
        public string IdSala { get { return idSala; } set { idSala = value; } }
        #endregion

        #region Constructor
        public DatosConexionMdl(Jugador jugador, string idSala)
        {
            this.jugador = jugador;
            this.idSala = idSala;
        }
        #endregion
    }
}
