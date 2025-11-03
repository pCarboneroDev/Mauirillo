using Cliente.Model;
using ENT;
using Mauirillo.Cliente.Viewmodels.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.ViewModels
{
    [QueryProperty(nameof(ListadoJugadores), "Listado")]
    public class FinalVM : INotifyPropertyChanged
    {
        #region Atributos
        private List<Jugador> listadoJugadores;
        private DelegateCommand salirCommand;
        #endregion

        #region Propiedades
        public List<Jugador> ListadoJugadores
        {
            get { return listadoJugadores; }
            set
            {
                listadoJugadores = value;
                listadoJugadores.Sort((x, y) => y.Puntuacion.CompareTo(x.Puntuacion)); // Orden descendente
                NotifyPropertyChanged();
            }
        }
        public DelegateCommand SalirCommand { get { return salirCommand; } }
     
        #endregion

        #region Constructores
        public FinalVM()
        {

            if (listadoJugadores != null)
            {
                listadoJugadores.Sort((x, y) => x.Puntuacion.CompareTo(y.Puntuacion));
                NotifyPropertyChanged(nameof(ListadoJugadores));
            }
            salirCommand = new DelegateCommand(salirExecute);
        }
        #endregion

        #region Commands
        /// <summary>
        /// Navega de regreso a la vista principal (HomeView).
        /// Pre: La navegación debe estar configurada correctamente en la aplicación.
        /// Post: Se redirige al usuario a la pantalla de inicio.
        /// </summary>
        private async void salirExecute()
        {
            await Shell.Current.GoToAsync("///HomeView");
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
