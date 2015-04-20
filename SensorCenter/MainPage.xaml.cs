using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;

namespace SensorCenter
{
    public partial class MainPage : PhoneApplicationPage
    {

        Accelerometer accelerometer;
        Boolean accSuported;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Establecer el contexto de datos del control ListBox control en los datos de ejemplo
            //DataContext = App.ViewModel;

            // Asociar appbarbuttons con el modelo
            appBarButtonPlay = (ApplicationBarIconButton)ApplicationBar.Buttons[0];            
            appBarButtonPause = (ApplicationBarIconButton)ApplicationBar.Buttons[1];            

            // Si el acelerometro no está disponible
            if (!Accelerometer.IsSupported)
            {
                accelerometerStatusTB.Text = "Acelerometro no soportado";
                accSuported = false;
            }
            else
            {
                accelerometerStatusTB.Text = "Acelerometro soportado";
                accSuported = true;
            }
        }

        // Cargar datos para los elementos ViewModel
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            /*
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
            */
        }
        
        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int currentIndex = panorama.SelectedIndex;
            switch (currentIndex)
            {
                case 1:
                    appBarButtonEnabler(accSuported);
                    break;
            }
        }

        private void appBarButtonEnabler(Boolean isEnab)
        {   
            if (isEnab)
            {
                appBarButtonPause.IsEnabled = true;
                appBarButtonPlay.IsEnabled = true;
            }
            else
            {
                appBarButtonPause.IsEnabled = false;
                appBarButtonPlay.IsEnabled = false;
            }
        }

        private void appBarButtonPause_Click(object sender, EventArgs e)
        {
            int currentIndex = panorama.SelectedIndex;
            switch(currentIndex)
            {
                case 1:
                    if (accelerometer != null)
                    {
                        // Detener el acelerometro
                        accelerometer.Stop();
                        accelerometerStatusTB.Text = "Detenido.";
                    }
                    break;
            }

        }

        private void appBarButtonPlay_Click(object sender, EventArgs e)
        {
            int currentIndex = panorama.SelectedIndex;
            switch(currentIndex)
            {
                case 1:
                    if (accelerometer == null)
                    {
                        accelerometer = new Accelerometer();
                        accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                        accelerometer.CurrentValueChanged +=
                            new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                    }
                    // Inicia el sensor
                    try
                    {
                        accelerometerStatusTB.Text = "Iniciando acelerometro.";
                        accelerometer.Start();
                    }
                    catch (InvalidOperationException ex)
                    {
                        accelerometerStatusTB.Text = "Error al inicializar";
                    }
                    break;
            }
        }

        private void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Dispatcher.BeginInvoke(() => UpdateUI(e.SensorReading));
        }

        private void UpdateUI(AccelerometerReading accelerometerReading)
        {
            accelerometerStatusTB.Text = "Obteniendo datos";

            Vector3 acceleration = accelerometerReading.Acceleration;

            // Cambiar valores visuales
            xAccelerometerTB.Text = "X: " + acceleration.X.ToString("0.00");
            yAccelerometerTB.Text = "Y: " + acceleration.Y.ToString("0.00");
            zAccelerometerTB.Text = "Z: " + acceleration.Z.ToString("0.00");

            xAcceleratorLine.X2 = xAcceleratorLine.X1 + acceleration.X * 200;
            yAcceleratorLine.Y2 = yAcceleratorLine.Y1 - acceleration.Y * 200;
            zAcceleratorLine.X2 = zAcceleratorLine.X1 - acceleration.Z * 100;
            zAcceleratorLine.Y2 = zAcceleratorLine.Y1 + acceleration.Z * 100;

        }

    }
}