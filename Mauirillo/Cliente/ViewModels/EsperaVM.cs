using Cliente.Model;
using Mauirillo.Cliente.Viewmodels.Utilities;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cliente.ViewModels
{
    [QueryProperty(nameof(DatosConexion), "Datos")]
    public class EsperaVM : Notify
    {
        #region Atributos
        private readonly HubConnection connection;
        private DatosConexionMdl datosConexion;
        private string idSala;
        #endregion

        #region Propiedades
        public DatosConexionMdl DatosConexion { 
            get { return datosConexion; }  
            set { datosConexion = value; NotifyPropertyChanged(); comprobarJugadores(); } 
        }

        #endregion

        public EsperaVM()
        {
            // Construcción de la conexión con el hub de SignalR
            connection = new HubConnectionBuilder()
                .WithUrl("https://mauirilloservidor.azurewebsites.net/hub") // URL del servidor en producción (comentado)
                //.WithUrl("http://localhost:5178/hub") // URL del servidor local para pruebas
                .Build();

            // Evento que se activa cuando el servidor indica que la partida debe comenzar
            connection.On("ComenzarPartida", () =>
            {
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Crea un diccionario para pasar datos a la nueva vista
                    Dictionary<string, object> diccionarioSala = new Dictionary<string, object>();
                    diccionarioSala.Add("Datos", datosConexion);

                    // El jugador abandona el grupo en el servidor antes de cambiar de vista
                    await connection.InvokeCoreAsync("LeaveGrupo", args: new[] { datosConexion.IdSala });

                    // Navega a la vista de juego (PintorView), pasando los datos de la sala
                    await Shell.Current.GoToAsync("///PintorView", diccionarioSala);
                });
            });

            // Inicia la conexión con el servidor
            conexion();

            //comprobarJugadores(); // Comprobación de jugadores (comentado, posiblemente para pruebas o futura activación)
        }


        #region Métodos
        /// <summary>
        /// Establece la conexión con el hub de SignalR.
        /// Pre: La conexión no debe estar ya iniciada.
        /// Post: Se inicia la conexión con el servidor.
        /// </summary>
        private async void conexion()
        {
            await connection.StartAsync();
        }

        /// <summary>
        /// Une al jugador a la sala y verifica el estado de los jugadores en la partida.
        /// Pre: Debe existir una sala válida y una conexión activa con el servidor.
        /// Post: El jugador se une a la sala y se comprueba la lista de jugadores.
        /// </summary>
        private async void comprobarJugadores()
        {
            await connection.InvokeCoreAsync("JoinGrupo", args: new[] { datosConexion.IdSala });
            await connection.InvokeCoreAsync("ComprobarJugadores", args: new[] { datosConexion.IdSala });
        }
        #endregion

    }
}
