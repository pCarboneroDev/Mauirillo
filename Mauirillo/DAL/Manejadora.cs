using DTO;
using ENT;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DAL
{
    public class Manejadora
    {

        private static List<Sala> listadoSalas = new List<Sala>();

        /// <summary>
        /// Función que devuelve el listado de las salas que hay en el juego
        /// </summary>
        /// Pre: Siempre devuelve 8 salas
		/// Post: Nunca puede no haber salas
        /// <returns>Devuelve una lista de salas</returns>
        public static List<Sala> getSalas()
        {
            return listadoSalas;
        }

        /// <summary>
        /// Funcion que recoge la sala que le manda el servidor
        /// </summary>
        /// <param name="sala"></param>
        public static void crearSala(Sala sala)
        {
            listadoSalas.Add(sala);
        }

        //public static void actJugadoresEnSala()
        //{

        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jugadores"></param>
        /// <returns></returns>
        public static async Task<List<Ronda>> getRondas(List<Jugador> jugadores)
        {
            List<string> palabras = await getPalabras();
            List<Ronda> rondas = new();
            Random rand = new Random();
            for (int i = 0; i < jugadores.Count; i++) 
            {
                Ronda r = new Ronda(jugadores, palabras[i], jugadores[i]);
                rondas.Add(r);
            }

            int n = jugadores.Count;
            for (int i = n - 1; i > 0; i--)
            {
                // Seleccionar un índice aleatorio entre 0 e i
                int j = rand.Next(i + 1);

                // Intercambiar los elementos en los índices i y j
                Ronda temp = rondas[i];
                rondas[i] = rondas[j];
                rondas[j] = temp;
            }

            return rondas;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async static Task<List<string>> getPalabras()
        {
            List<string> palabras = new();
            List<string> aux = new();
            string bearerToken = "$2y$10$agviBTdMAB6UcTZzrHjyP.vygRt9dxjuW1.g/Pb9fZGmNuZ1PP52S";
            Uri url = new Uri("https://www.freeapidatabase.com/View/methodget.php?idTbla=522&idProyect=184");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            HttpResponseMessage miCodigoRespuesta;
            string textoJsonRespuesta;
            try
            {
                miCodigoRespuesta = await httpClient.GetAsync(url);

                if (miCodigoRespuesta.IsSuccessStatusCode)
                {
                    textoJsonRespuesta = await httpClient.GetStringAsync(url);

                    string jsonResponse = await miCodigoRespuesta.Content.ReadAsStringAsync();

                    // Parsear JSON con JsonDocument
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;
                    JsonElement results = root.GetProperty("results");

                    foreach (JsonElement element in results.EnumerateArray())
                    {
                        string palabra = element.GetProperty("Palabra").GetString();
                        aux.Add(palabra);
                    }

                    while(palabras.Count < 5)
                    {
                        Random random = new Random();
                        string p = aux[random.Next(aux.Count-1)];
                        if (!palabras.Contains(p))
                        {
                            palabras.Add(p);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return palabras;
        }

    }
}
