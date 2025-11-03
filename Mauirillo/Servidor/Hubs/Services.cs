using DAL;
using DTO;
using ENT;
using Microsoft.AspNetCore.SignalR;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Servidor.Hubs
{
    public class Services : Hub
    {
        #region Atributos
        private static List<Sala> listadoSalas = new List<Sala>();
        private static IDictionary<string, Jugador> diccJugadores;
        #endregion

        #region Propiedades
        public static List<Sala> ListadoSalas
        {
            get { return listadoSalas; }
            set { listadoSalas = value; }
        }
        public static IDictionary<string, Jugador> DiccJugadores
        {
            get { return diccJugadores; }
            set { diccJugadores = value; }
        }
        #endregion

        #region Constructores
        public Services()
        {
            //listadoSalas = new List<Sala>();
        }
        #endregion

        #region Grupos
        /// <summary>
        /// Función que añade al usuario a un grupo
        /// </summary>
        /// <param name="grupo">Nombre del grupo</param>
        /// <returns>Conexión al grupo del servidor</returns>
        public Task JoinGrupo(string grupo)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, grupo);
        }

        /// <summary>
        /// Función que elimina al usuario de un grupo
        /// </summary>
        /// <param name="grupo">Nombre del grupo</param>
        /// <returns>Desconexión al grupo del servidor</returns>
        public Task LeaveGrupo(string grupo)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
        }
        #endregion

        #region Salas
        /// <summary>
        /// Función que actualiza la lista de salas del servidor al unirse un jugador a esta
        /// </summary>
        /// <param name="sala">Sala que se quiere unir el usuario</param>
        /// <param name="jugador">Jugador que se añade a la sala</param>
        /// <returns>El Id del jugador en la sala</returns>
        public async Task<int> actualizarSalas(Sala sala, Jugador jugador)
        {
            Sala salaB = listadoSalas.Find(x => x.IdSala == sala.IdSala);

            jugador.IdJugador = salaB.Jugadores.Count;

            salaB.Jugadores.Add(jugador); // esto modifica la sala de la lista o es una sala clonada

            await sendSalas();
            return jugador.IdJugador;
        }

        /// <summary>
        /// Función que crea una sala en la lista de salas del servidor
        /// </summary>
        /// <param name="sala">Sala que se quiere añadir</param>
        /// <returns>Lista de salas actualizada</returns>
        public async Task crearSala(Sala sala)
        {
            sala.IdSala = buscarId(listadoSalas);

            listadoSalas.Add(sala);

            await sendSalas();
        }

        /// <summary>
        /// Función que envia la lista de salas del servidor
        /// </summary>
        /// <returns>Lista de salas</returns>
        public async Task sendSalas()
        {
            await Clients.All.SendAsync("RecieveSalas", listadoSalas);
        }

        /// <summary>
        /// Función que actualiza la lista de salas del servidor
        /// </summary>
        /// <returns>Lista de salas</returns>
        public async Task loadSalas()
        {
            await Clients.Caller.SendAsync("RecieveSalas", listadoSalas);
        }

        /// <summary>
        /// Funcion que sirve para añadirle un id a la sala, 
        /// este recoge el ultimo index de la lista de salas, le suma 1 y la retorna
        /// </summary>
        /// <param name="salas">Lista de salas del servidor</param>
        /// <returns>Id perteneciente a la nueva sala</returns>
        private int buscarId(List<Sala> salas)
        {
            int lastIndex;
            if (salas == null || salas.Count == 0)
            {
                lastIndex = 0;
            }
            else
            {
                lastIndex = salas.Max(s => s.IdSala);
            }

            return lastIndex + 1;
        }

        /// <summary>
        /// Función que borra la sala de la lista de salas
        /// </summary>
        /// <param name="sala">Nombre de la sala que se quiere borrar</param>
        /// <returns>La lista de salas actualizada</returns>
        public async Task DeleteSala(string sala)
        {
            Sala s = listadoSalas.Find(sa => sa.IdSala == int.Parse(sala));

            if (s != null)
            {
                listadoSalas.Remove(s);
            }
            await Clients.All.SendAsync("RecieveSalas", listadoSalas);
        }

        #endregion

        /// <summary>
        /// Función que comprueba cuantos jugadores hay en la sala
        /// </summary>
        /// <param name="grupoId">Nombre de la sala</param>
        /// <returns>Si hay 4 jugadores en la sala se manda una señal para que comience la partida</returns>
        public async Task comprobarJugadores(string grupoId)
        {
            Sala s = listadoSalas.Find(x => x.IdSala == int.Parse(grupoId));

            if (s.Jugadores.Count == 4)
            {
                await Clients.Group(grupoId).SendAsync("ComenzarPartida");
            }
        }

        #region Partida
        /// <summary>
        /// Función que envia el dibujo actualizado a los adivinadores
        /// </summary>
        /// <param name="lista">Lista de listas de puntos de la pizarra</param>
        /// <param name="grupo">Nombre del grupo</param>
        /// <returns></returns>
        public async Task enviarDibujo(List<List<PointF>> lista, string grupo)
        {
            await Clients.OthersInGroup(grupo).SendAsync("RecieveDibujo", lista);
        }

        /// <summary>
        /// Función que genera las rondas para la partida
        /// </summary>
        /// <param name="grupo">Nombre del grupo</param>
        /// <returns>Lista de rondas</returns>
        public async Task generaRondas(string grupo)
        {
            List<Ronda> rondas = await Manejadora.getRondas(listadoSalas.Find(s => s.IdSala == int.Parse(grupo)).Jugadores);
            await Clients.Group(grupo).SendAsync("GetRondas", rondas);
        }

        /// <summary>
        /// Función que cambia de ronda de la lista de rondas
        /// </summary>
        /// <param name="g">Nombre del grupo</param>
        /// <returns>Señal que hace que cambie de ronda los jugadores en el ViewModel</returns>
        public async Task cambiaRonda(string g)
        {
            await Clients.Group(g).SendAsync("OtherRound");
        }

        /// <summary>
        /// Función que indica quienes han sido los jugadores que reciben puntos
        /// </summary>
        /// <param name="idJugador">Id del jugador que ha adivinado</param>
        /// <param name="idPintor">Id del jugador que ha pintado</param>
        /// <param name="grupo">Nombre del grupo</param>
        /// <returns>Lista con los ids de los jugadores que reciben puntos</returns>
        public async Task sumarPuntos(int idJugador, int idPintor, string grupo)
        {
            List<int> listaIds = new List<int>
            {
                idJugador, idPintor
            };
            await Clients.Group(grupo).SendAsync("SumarPuntos", listaIds);
        }

        /// <summary>
        /// Función que manda un mensaje, del servidor o de un jugador, a todos los jugadores de la sala
        /// </summary>
        /// <param name="m">Mensaje que se quiere enviar</param>
        /// <param name="sala">Nombre de la sala</param>
        /// <returns>Mensaje enviado</returns>
        public async Task Chat(Mensaje m, string sala)
        {
            await Clients.Group(sala).SendAsync("ActChat", m);
        }
        #endregion
    }
}
