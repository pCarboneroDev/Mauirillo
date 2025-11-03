using Cliente.Model;
using DAL;
using ENT;
using Mauirillo.Cliente.Viewmodels.Utilities;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cliente.ViewModels
{
    public class HomeVM: Notify
    {
        #region Atributos
        private readonly HubConnection connection;
        private string nombreJugador;
        private ObservableCollection<Sala> listadoSalas;
        private Jugador jugador;
        private DelegateCommand unirseCommand;
        private DelegateCommand crearSalaCommand;
        private Sala salaSeleccionada;
        private string nombreSala;
        private DatosConexionMdl datosConexion;
        #endregion

        #region Propiedades
        public string NombreJugador
        {
            get { return nombreJugador; }
            set { nombreJugador = value; unirseCommand.RaiseCanExecuteChanged(); }
        }

        public ObservableCollection<Sala> ListadoSalas
        {
            get { return listadoSalas; }
        }

        public DelegateCommand UnirseCommand
        {
            get { return unirseCommand; }
        }

        public DelegateCommand CrearSalaCommand
        {
            get { return crearSalaCommand; }
        }

        public Sala SalaSeleccionada
        {
            get { return salaSeleccionada; }
            set { salaSeleccionada = value; unirseCommand.RaiseCanExecuteChanged(); }
        }

        public string NombreSala
        {
            get { return nombreSala; }
            set { nombreSala = value; crearSalaCommand.RaiseCanExecuteChanged(); }
        }


        #endregion

        #region Constructor
        public HomeVM()
        {
            // Obtiene la lista de salas desde la manejadora y la asigna a la colección observable
            listadoSalas = new ObservableCollection<Sala>(Manejadora.getSalas());

            // Inicializa los comandos para unirse a una sala y crear una nueva
            unirseCommand = new DelegateCommand(unirseExecute, unirseCanExecute);
            crearSalaCommand = new DelegateCommand(crearSalaExecute, crearSalaCanExecute);

            // Configuración de la conexión con el servidor de SignalR
            connection = new HubConnectionBuilder()
                .WithUrl("https://mauirilloservidor.azurewebsites.net/hub")
                //.WithUrl("http://localhost:5178/hub")// URL en entorno local para pruebas
                .Build();

            // Evento que recibe la lista de salas desde el servidor
            connection.On<List<Sala>>("RecieveSalas", (salas) =>
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Actualiza la lista de salas con la información recibida
                    listadoSalas = new ObservableCollection<Sala>(salas);
                    NotifyPropertyChanged("ListadoSalas"); // Notifica el cambio a la interfaz de usuario
                });
            });
            connection.Closed += async (error) =>
            {
                Console.WriteLine("⚠ Conexión cerrada. Reintentando...");
                await Task.Delay(2000); // Espera antes de reconectar
                await reconectar();
            };

            // Inicia la conexión con el servidor
            conexion();
        }

        #endregion

        #region metodos
        /// <summary>
        /// Funcion que realiza la conexion al hub
        /// </summary>
        private async void conexion()
        {
            await connection.StartAsync();
            await connection.InvokeCoreAsync("loadSalas", args: []);
        }
        #endregion

        #region Commands
        private async void unirseExecute()
        {
            jugador = new Jugador(nombreJugador);
            jugador.IdJugador = await connection.InvokeCoreAsync<int>("actualizarSalas", args: new object[] { salaSeleccionada, jugador } );
            NotifyPropertyChanged("ListadoSalas");

            datosConexion = new DatosConexionMdl(jugador, salaSeleccionada.IdSala.ToString());

            Dictionary<string, object> diccionarioSala = new Dictionary<string, object>();

            diccionarioSala.Add("Datos", datosConexion);

            await Shell.Current.GoToAsync("///EsperaView", diccionarioSala);
            salaSeleccionada = null;
        }

        private bool unirseCanExecute()
        {
            bool canExecute = false;

            if (salaSeleccionada != null && !String.IsNullOrEmpty(nombreJugador) && salaSeleccionada.Jugadores.Count < 4)
            {
                canExecute = true;
            }

            return canExecute;
        }

        private async void crearSalaExecute()
        {
            Sala sala = new Sala(nombreSala, new List<Jugador>());
            Sala salaCreada = listadoSalas.ToList().Find(x => x.Nombre == sala.Nombre);

            if (salaCreada != null )
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Ya existe una sala con ese nombre", "Aceptar");
            }
            else
            {
            await connection.InvokeCoreAsync("crearSala",  args: new[] { sala });         
            }
        }

        private bool crearSalaCanExecute()
        {
            bool canExecute = false;
            if (!String.IsNullOrEmpty(NombreSala))
            {
                canExecute = true;
            }
            return canExecute;
        }

        public async Task reconectar()
        {
            if (connection.State == HubConnectionState.Disconnected)
            {
                conexion();
            }
        }
        
        #endregion
    }
}
