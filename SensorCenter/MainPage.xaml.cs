using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using System.Threading.Tasks;
using Windows.Devices.Geolocation;

using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace SensorCenter
{
    public partial class MainPage : PhoneApplicationPage
    {

        Geolocator geolocator = new Geolocator();
        Boolean geoTracking = false;

        Accelerometer accelerometer;
        Boolean accSuported;
        Boolean accActive = false;

        Gyroscope gyroscope;
        DispatcherTimer timer;
        Boolean gyroSupported;
        Boolean gyroActive = false;        
        Vector3 currentRotationRate = Vector3.Zero;
        Vector3 cumulativeRotation = Vector3.Zero;
        DateTimeOffset lastUpdateTime = DateTimeOffset.MinValue;
        

        // Constructor
        public MainPage()
        {
            InitializeComponent();

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

            // Si el giroscopio no está disponible
            if (!Gyroscope.IsSupported)
            {
                gyroStatusTB.Text = "Giroscopio no soportado.";
                gyroSupported = false;
            }
            else
            {
                gyroStatusTB.Text = "Giroscopio soportado.";
                gyroSupported = true;
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(60);
                timer.Tick += new EventHandler(Gyro_timer_Tick);
            }
        }

        // Permiso del usuario de usar el sericio de ubicación
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") ) {
                // User has opted in or out of Location                
                return;
            }
            else
            {                
                MessageBoxResult result =
                    MessageBox.Show("Esta aplicación necesita hacer uso de tu ubicación.¿Estás de acuerdo?",
                    "Location",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;                    
                }
                else
                {                    
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;                    
                }

                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }
        
        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int currentIndex = panorama.SelectedIndex;
            switch (currentIndex)
            {   case 0:
                    appBarButtonEnabler(true);
                    break;
                case 1:
                    appBarButtonEnabler(accSuported);
                    break;
                case 2:
                    appBarButtonEnabler(gyroSupported);
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
                case 0:
                    if (((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == true) && (geoTracking))
                    {
                        geolocator.PositionChanged -= geolocator_PositionChanged;
                        geolocator.StatusChanged -= geolocator_StatusChanged;
                        geolocator = null;

                        geoTracking = false;
                        geoStatusTB.Text = "Detenido.";

                    }
                    break;
                case 1:
                    if  ((accelerometer != null) && (accActive))
                    {
                        // Detener el acelerometro
                        accActive = false;
                        accelerometer.Stop();
                        accelerometerStatusTB.Text = "Detenido.";
                    }
                    break;
                case 2:
                    if ((gyroscope != null) && gyroscope.IsDataValid && gyroActive)
                    {
                        // Detener el giroscopio
                        gyroActive = false;
                        gyroscope.Stop();
                        timer.Stop();
                        gyroStatusTB.Text = "Detenido";                        
                    }
                    break;
            }

        }        

        private void appBarButtonPlay_Click(object sender, EventArgs e)
        {
            int currentIndex = panorama.SelectedIndex;
            switch(currentIndex)
            {
                case 0:
                    if ( ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == true) && (geoTracking==false))
                    {
                        //Si hay permisos para usar la localización
                        geolocator = new Geolocator();
                        geolocator.DesiredAccuracy = PositionAccuracy.High;
                        geolocator.MovementThreshold = 100; //metros

                        geolocator.StatusChanged += geolocator_StatusChanged;
                        geolocator.PositionChanged += geolocator_PositionChanged;
                        geoTracking = true;
                        geoStatusTB.Text = "Geolocalización activa";
                    }
                    break;

                case 1:
                    if (accActive == false)
                    {
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
                            accActive = true;
                            accelerometerStatusTB.Text = "Iniciando acelerometro.";
                            accelerometer.Start();
                        }
                        catch (InvalidOperationException ex)
                        {
                            accelerometerStatusTB.Text = "Error al inicializar";
                        }
                    }                    
                    break;
                case 2:                    
                    if (gyroActive == false)
                    {
                        if (gyroscope == null)
                        {
                            gyroscope = new Gyroscope();
                            gyroscope.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20); // SE podría modificar el valor de espera
                            gyroTimeBetweenUpdatesTB.Text = "Tiempo entre actualizaciones: " + gyroscope.TimeBetweenUpdates.TotalMilliseconds +"ms";
                            gyroscope.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<GyroscopeReading>>(gyroscope_CurrentValueChanged);
                        }
                        //Inicia giroscopio
                        try
                        {
                            gyroActive = true;
                            gyroStatusTB.Text = "Iniciando giroscopio.";
                            gyroscope.Start();
                            timer.Start();
                        }
                        catch (InvalidOperationException ex)
                        {
                            gyroStatusTB.Text = "Error al iniciar.";
                        }
                    }
                    break;
            }
        }

        /*        
         * GEOLOCATOR METHODS         
         */
        private void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            String status = "";
            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "ubicación deshabilitada";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "inicializando";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "sin datos";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "listo";
                    break;
                case PositionStatus.NotAvailable:
                    status = "no disponible";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

                    break;
            }
            Dispatcher.BeginInvoke(() => geoStatusTB.Text = "Estado: "+status);
        }
        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Dispatcher.BeginInvoke(() =>
            {
                LatitudeTextBlock.Text = "Latitud: " + args.Position.Coordinate.Latitude.ToString("0.000000")+"°";
                LongitudeTextBlock.Text = "Longitud: " + args.Position.Coordinate.Longitude.ToString("0.000000")+"°";
            });
        }

        /*        
        * ACCELEROMETER METHODS         
        */
        private void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Dispatcher.BeginInvoke(() => AccelerometerUpdateUI(e.SensorReading));
        }

        private void AccelerometerUpdateUI(AccelerometerReading accelerometerReading)
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

        /*
         * GYROSCOPE METHODS
         */
        void gyroscope_CurrentValueChanged(object sender, SensorReadingEventArgs<GyroscopeReading> e)
        {
            if (lastUpdateTime.Equals(DateTimeOffset.MinValue))
            {
                // If this is the first time CurrentValueChanged was raised,
                // only update the lastUpdateTime variable.
                lastUpdateTime = e.SensorReading.Timestamp;
            }else
            {
                // Get the current rotation rate. This value is in 
                // radians per second.
                currentRotationRate = e.SensorReading.RotationRate;

                // Subtract the previous timestamp from the current one
                // to determine the time between readings
                TimeSpan timeSinceLastUpdate = e.SensorReading.Timestamp - lastUpdateTime;

                // Obtain the amount the device rotated since the last update
                // by multiplying by the rotation rate by the time since the last update.
                // (radians/second) * secondsSinceLastReading = radiansSinceLastReading
                cumulativeRotation += currentRotationRate * (float)(timeSinceLastUpdate.TotalSeconds);

                lastUpdateTime = e.SensorReading.Timestamp;
            }
        }

        void Gyro_timer_Tick(object sender, EventArgs e)
        {
            if (gyroscope.IsDataValid)
            {
                gyroStatusTB.Text = "Recibiendo datos del giroscopio.";
            }

            gyroCurrentXTB.Text = currentRotationRate.X.ToString("0.000");
            gyroCurrentYTB.Text = currentRotationRate.Y.ToString("0.000");
            gyroCurrentZTB.Text = currentRotationRate.Z.ToString("0.000");

            gyroCumulativeXTB.Text =
              MathHelper.ToDegrees(cumulativeRotation.X).ToString("0.00");
            gyroCumulativeYTB.Text =
              MathHelper.ToDegrees(cumulativeRotation.Y).ToString("0.00");
            gyroCumulativeZTB.Text =
              MathHelper.ToDegrees(cumulativeRotation.Z).ToString("0.00");           
        }


    }
}