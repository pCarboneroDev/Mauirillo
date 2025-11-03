using Cliente.Model;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using DTO;
using ENT;
using Mauirillo.Cliente.Viewmodels.Utilities;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cliente.ViewModels
{
    [QueryProperty(nameof(Datos), "Datos")]
    public class PartidaVM : GraphicsView, INotifyPropertyChanged
    {
        #region Atributos
        private readonly HubConnection _hubConnection;
        private List<Ronda> listaRondas;
        private Ronda rondaActual;
        private ObservableCollection<IDrawingLine> pizarra;
        private string palabra;
        private string palabraAdivinar;
        private DelegateCommand enviarCommand;
        private bool isSending;
        private int listaIndex = 0;
        private DatosConexionMdl datos;
        private DelegateCommand borrarPizarra;

        private bool soyPintor;
        //private bool noSoyPintor;

        private int numLetrasPalabraAdivinar;

        private int timer = 60;
        private int maxTime = 60;

        private int puntosPintor = 50;
        private int puntosJugador = 100;

        private DateTime lastSendTime = DateTime.UtcNow;
        private readonly TimeSpan minInterval = TimeSpan.FromMilliseconds(100);

        private ObservableCollection<Jugador> listaJugadores;
        private ObservableCollection<Mensaje> mensajesChat;
        #endregion

        #region Propiedades
        public List<Ronda> ListaRondas { get { return listaRondas; } }
        public string Palabra { get { return palabra; } set { palabra = value; enviarCommand.RaiseCanExecuteChanged(); } }
        public string PalabraAdivinar { get { return palabraAdivinar; } }
        public DelegateCommand EnviarCommand { get { return enviarCommand; } }
        public ObservableCollection<IDrawingLine> Pizarra { get { return pizarra; } set { pizarra = value; } }
        public DatosConexionMdl Datos { get { return datos; } set { datos = value; NotifyPropertyChanged(); iniciarPartida(); } }

        public DelegateCommand BorrarPizarra { get { return borrarPizarra; } }

        public bool SoyPintor { get { return soyPintor; } }

        public bool NoSoyPintor { get { return !soyPintor; } }

        public int Timer { get { return timer; } }

        public ObservableCollection<Jugador> ListaJugadores { get { return listaJugadores; } }
        public ObservableCollection<Mensaje> MensajesChat { get { return mensajesChat; } set { mensajesChat = value; NotifyPropertyChanged(); } }
        #endregion

        #region Constructores
        public PartidaVM()
        {
            mensajesChat = new();
            pizarra = new();
            enviarCommand = new DelegateCommand(enviarExecute, enviarCanExecute);
            borrarPizarra = new DelegateCommand(borrarExecute);
            // Construcción de la conexión con el hub de SignalR
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://mauirilloservidor.azurewebsites.net/hub")
                .Build();

            // Evento que recibe la lista de rondas desde el servidor
            _hubConnection.On<List<Ronda>>("GetRondas", (lista) =>
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Asigna la lista de rondas recibida a la variable local
                    listaRondas = lista;
                    rondaActual = listaRondas[listaIndex];

                    // Actualiza la lista de jugadores basada en la ronda actual
                    listaJugadores = new ObservableCollection<Jugador>(rondaActual.ListaJugadores);
                    NotifyPropertyChanged("ListaJugadores");

                    // Comprueba si el jugador actual es el pintor y actualiza la palabra a adivinar
                    comprobarPintor();
                    cambiarPalabra();
                });
            });

            // Evento que recibe los trazos del dibujo desde el servidor
            _hubConnection.On<List<List<PointF>>>("RecieveDibujo", (pointsLists) =>
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Limpia la pizarra antes de dibujar los nuevos trazos recibidos
                    pizarra.Clear();
                    foreach (var point in pointsLists)
                    {
                        // Crea una nueva línea de dibujo con los puntos recibidos y color blanco
                        IDrawingLine line = new DrawingLine { Points = new ObservableCollection<PointF>(point), LineColor = Colors.White };
                        pizarra.Add(line);
                    }
                });
            });

            // Evento que actualiza el chat con nuevos mensajes recibidos desde el servidor
            _hubConnection.On<Mensaje>("ActChat", (m) =>
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Agrega el mensaje recibido a la lista de mensajes del chat
                    MensajesChat.Add(m);
                });
            });

            // Evento que se activa cuando se debe cambiar a la siguiente ronda
            _hubConnection.On("OtherRound", () =>
            {
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Mensaje del servidor indicando la palabra correcta de la ronda anterior
                    Mensaje m = new Mensaje { Nombre = "Servidor", Msg = "La palabra era '" + rondaActual.Palabra + "'", Color = "Red" };
                    MensajesChat.Add(m);

                    if (rondaActual != listaRondas.Last()) // Si no es la última ronda
                    {
                        // Avanza a la siguiente ronda y actualiza la interfaz
                        listaIndex++;
                        rondaActual = listaRondas[listaIndex];
                        comprobarPintor();
                        cambiarPalabra();
                        pizarra.Clear();
                        NotifyPropertyChanged("Pizarra");

                        palabra = "";
                        NotifyPropertyChanged("Palabra");
                    }
                    else // Si la partida ha terminado
                    {
                        // El jugador abandona el grupo en el servidor
                        await _hubConnection.InvokeCoreAsync("LeaveGrupo", args: new[] { datos.IdSala });

                        // Limpia el chat
                        MensajesChat.Clear();

                        // Solicita al servidor eliminar la sala de juego
                        await _hubConnection.InvokeCoreAsync("DeleteSala", args: new[] { datos.IdSala });

                        // Prepara los datos de los jugadores para la pantalla final y navega a ella
                        Dictionary<string, object> diccionarioPartida = new Dictionary<string, object>();
                        List<Jugador> listaEnviar = new List<Jugador>(listaJugadores);
                        diccionarioPartida.Add("Listado", listaEnviar);
                        await Shell.Current.GoToAsync("///FinalView", diccionarioPartida);
                    }
                });
            });

            // Evento que actualiza la puntuación de los jugadores
            _hubConnection.On<List<int>>("SumarPuntos", (listaIds) =>
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Suma puntos al jugador que adivinó y al pintor de la ronda
                    listaJugadores.FirstOrDefault(j => j.IdJugador == listaIds[0]).Puntuacion += puntosJugador;
                    listaJugadores.FirstOrDefault(j => j.IdJugador == listaIds[1]).Puntuacion += puntosPintor;

                    // Refresca la lista de jugadores y notifica cambios en la interfaz
                    listaJugadores = new ObservableCollection<Jugador>(listaJugadores);
                    NotifyPropertyChanged(nameof(ListaJugadores));
                });
            });

            // Inicia la conexión con el servidor
            conectar();
        }

        #endregion

            /// <summary>
            /// Establece la conexión con el servidor a través del hub de SignalR.
            /// Pre: La conexión no debe estar ya establecida.
            /// Post: Se inicia la conexión con el servidor.
            /// </summary>
        private async void conectar()
        {
            await _hubConnection.StartAsync();
        }

        /// <summary>
        /// Inicia una partida uniéndose a un grupo de juego y, si el jugador es el indicado, genera las rondas.
        /// Pre: La conexión debe estar establecida y los datos del jugador deben ser válidos.
        /// Post: El jugador se une a la sala y, si es el jugador con ID 3, se generan las rondas.
        /// </summary>
        private async void iniciarPartida()
        {
            await _hubConnection.InvokeCoreAsync("JoinGrupo", args: new[] { datos.IdSala });
            if (datos.Jugador.IdJugador == 3)
            {
                await _hubConnection.InvokeCoreAsync("generaRondas", args: new[] { datos.IdSala });
            }
        }

        #region Dibujo
        /// <summary>
        /// Envía los datos del dibujo al servidor para su sincronización con otros jugadores.
        /// Pre: La lista de trazos no debe estar vacía y debe respetarse un intervalo mínimo entre envíos.
        /// Post: Los datos del dibujo se envían al servidor.
        /// </summary>
        private async Task SendDrawing()
        {
            if (isSending || Pizarra.Count == 0) return;

            var now = DateTime.UtcNow;
            if ((now - lastSendTime) < minInterval) return; // Evita enviar demasiados datos rápidamente

            lastSendTime = now;
            isSending = true;

            try
            {
                List<List<PointF>> pointsList = Pizarra.Select(path => path.Points.ToList()).ToList();
                await _hubConnection.InvokeCoreAsync("enviarDibujo", args: new object[] { pointsList, datos.IdSala });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                isSending = false;
            }
        }

        /// <summary>
        /// Método que se ejecuta cuando el dibujo cambia, enviándolo al servidor.
        /// Pre: Debe haber un dibujo válido en la pizarra.
        /// Post: Llama a SendDrawing para sincronizar el dibujo.
        /// </summary>
        public async void OnDrawingChanged()
        {
            await SendDrawing();
        }
        #endregion

        /// <summary>
        /// Comprueba si el jugador actual es el pintor de la ronda y actualiza el estado correspondiente.
        /// Pre: Debe existir una ronda en curso con un pintor asignado.
        /// Post: Se actualiza el estado de "soyPintor" y se notifican los cambios.
        /// </summary>
        private void comprobarPintor()
        {
            //string nombrePintor = rondaActual.Pintor.Nombre;

            if (datos.Jugador.IdJugador == rondaActual.Pintor.IdJugador)
            {
                soyPintor = true;
            }
            else
            {
                soyPintor = false;
            }

            NotifyPropertyChanged("SoyPintor");
            NotifyPropertyChanged("NoSoyPintor");
        }

        /// <summary>
        /// Cambia la palabra a adivinar y actualiza su representación según el rol del jugador.
        /// Pre: Debe existir una ronda en curso con una palabra asignada.
        /// Post: Se actualiza la palabra visible para los jugadores y se notifican los cambios.
        /// </summary>
        private void cambiarPalabra()
        {
            palabraAdivinar = rondaActual.Palabra;
            numLetrasPalabraAdivinar = palabraAdivinar.Length;
            //comenzarTiempoAsync();

            if (!soyPintor)
            {
                palabraAdivinar = "";
                for (int i = 0; i < numLetrasPalabraAdivinar; i++)
                {
                    palabraAdivinar += "_ ";
                }
            }
            NotifyPropertyChanged("PalabraAdivinar");

            //if (rondaActual.Pintor.IdJugador == datos.Jugador.IdJugador)
            //{
            //    comenzarTiempoAsync();
            //}
        }


        //private async Task comenzarTiempoAsync()
        //{
        //    timer = maxTime;
        //    NotifyPropertyChanged(nameof(Timer));
        //    while (timer > 0)
        //    {
        //        await Task.Delay(1000); // Espera 1 segundo sin bloquear
        //        timer -= 1;
        //        NotifyPropertyChanged("Timer");
        //    }
        //    await _hubConnection.InvokeCoreAsync("cambiaRonda", args: new[] { datos.IdSala });
        //}

        #region Commands
        /// <summary>
        /// Envía un mensaje al chat y verifica si la palabra ingresada es correcta para sumar puntos y cambiar de ronda.
        /// Pre: Debe haber una conexión activa y una palabra ingresada por el usuario.
        /// Post: El mensaje se envía al chat y, si la palabra es correcta, se otorgan puntos y se cambia de ronda.
        /// </summary>
        private async void enviarExecute()
        {
            //await _hubConnection.InvokeCoreAsync("cambiaRonda", args: new[] {datos.IdSala});
            Mensaje m = new Mensaje { Nombre = datos.Jugador.Nombre, Msg = palabra, Color = "Black" };
            await _hubConnection.InvokeCoreAsync("Chat", args: new object[] { m, datos.IdSala });

            if (palabra.Equals(rondaActual.Palabra))
            {
                await _hubConnection.InvokeCoreAsync("sumarPuntos", args: new object[] { datos.Jugador.IdJugador, rondaActual.Pintor.IdJugador, datos.IdSala });
                //ListaJugadores.Find(j => j.IdJugador == datos.Jugador.IdJugador).Puntuacion += puntosJugador;
                //ListaJugadores.Find(j => j.IdJugador == rondaActual.Pintor.IdJugador).Puntuacion += puntosPintor;

                await _hubConnection.InvokeCoreAsync("cambiaRonda", args: new[] { datos.IdSala });
            }

            palabra = "";
            NotifyPropertyChanged(nameof(Palabra));
        }

        /// <summary>
        /// Determina si el botón de enviar mensaje debe estar habilitado.
        /// Pre: Ninguna.
        /// Post: Devuelve true si hay una palabra ingresada, false en caso contrario.
        /// </summary>
        private bool enviarCanExecute()
        {
            bool enviar = false;
            if (!String.IsNullOrEmpty(palabra))
            {
                enviar = true;
            }
            return enviar;
        }

        /// <summary>
        /// Borra la pizarra y notifica al servidor para actualizar la pantalla de todos los jugadores.
        /// Pre: Debe haber una conexión activa con el servidor.
        /// Post: La pizarra local se limpia y los datos vacíos se envían al servidor.
        /// </summary>
        private async void borrarExecute()
        {
            pizarra.Clear();

            try
            {
                List<List<PointF>> pointsList = Pizarra.Select(path => path.Points.ToList()).ToList();
                await _hubConnection.InvokeCoreAsync("enviarDibujo", args: new object[] { pointsList, datos.IdSala });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }
        #endregion

        #region Notify

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new
            PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
