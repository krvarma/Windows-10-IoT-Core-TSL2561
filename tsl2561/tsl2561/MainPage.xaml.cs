using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace tsl2561
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // I2C Controller name
        private const string I2C_CONTROLLER_NAME = "I2C1";

        // I2C Device
        private I2cDevice I2CDev;
        // Timer
        private DispatcherTimer ReadSensorTimer;
        // TSL Sensor
        private TSL2561 TSL2561Sensor;

        // TSL Gain and MS Values
        private Boolean Gain = false;
        private uint MS = 0;

        // Http Server
        private HttpServer WebServer = null;

        // Holds current luminosity
        private static double CurrentLux = 0;

        public MainPage()
        {
            this.InitializeComponent();

            /* Register for the unloaded event so we can clean up upon exit */
            Unloaded += MainPage_Unloaded;

            // Initialize I2C Device
            InitializeI2CDevice();

            // Start Timer every 3 seconds
            ReadSensorTimer = new DispatcherTimer();
            ReadSensorTimer.Interval = TimeSpan.FromMilliseconds(3000);
            ReadSensorTimer.Tick += Timer_Tick;
            ReadSensorTimer.Start();

            // Initialize and Start HTTP Server
            WebServer = new HttpServer();

            var asyncAction = ThreadPool.RunAsync((w) => { WebServer.StartServer(); });
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            I2CDev.Dispose();
            WebServer.Dispose();
        }

        private async void InitializeI2CDevice()
        {
            try
            {
                // Initialize I2C device
                var settings = new I2cConnectionSettings(TSL2561.TSL2561_ADDR);

                settings.BusSpeed = I2cBusSpeed.FastMode;
                settings.SharingMode = I2cSharingMode.Shared;

                string aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */

                I2CDev = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());

                return;
            }

            initializeSensor();
        }

        private void initializeSensor()
        {
            // Initialize Sensor
            TSL2561Sensor = new TSL2561(ref I2CDev);

            // Set the TSL Timing
            MS = (uint)TSL2561Sensor.SetTiming(false, 2);
            // Powerup the TSL sensor
            TSL2561Sensor.PowerUp();

            Debug.WriteLine("TSL2561 ID: " + TSL2561Sensor.GetId());
        }

        private void Timer_Tick(object sender, object e)
        {
            // Retrive luminosity and update the screen
            uint[] Data = TSL2561Sensor.GetData();

            Debug.WriteLine("Data1: " + Data[0] + ", Data2: " + Data[1]);

            CurrentLux = TSL2561Sensor.GetLux(Gain, MS, Data[0], Data[1]);

            String strLux = String.Format("{0:0.00}", CurrentLux);
            String strInfo = "Luminosity: " + strLux + " lux";

            Debug.WriteLine(strInfo);

            LightValue.Text = strInfo;
        }
        
        // Http Server class
        public sealed class HttpServer : IDisposable
        {
            private const uint bufLen = 8192;
            private int defaultPort = 8080;
            private readonly StreamSocketListener sock;

            public object[] TimeStamp { get; private set; }

            public HttpServer()
            {
                sock = new StreamSocketListener();

                sock.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
            }

            public async void StartServer()
            {
                await sock.BindServiceNameAsync(defaultPort.ToString());
            }

            private async void ProcessRequestAsync(StreamSocket socket)
            {
                // Read in the HTTP request, we only care about type 'GET'
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    byte[] data = new byte[bufLen];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = bufLen;
                    while (dataRead == bufLen)
                    {
                        await input.ReadAsync(buffer, bufLen, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    string requestMethod = request.ToString().Split('\n')[0];
                    string[] requestParts = requestMethod.Split(' ');
                    await WriteResponseAsync(requestParts, output);
                }
            }

            private async Task WriteResponseAsync(string[] requestTokens, IOutputStream outstream)
            {
                // Content body
                string respBody = string.Format(@"<html>
                                                    <head>
                                                        <title>Luminosity Sensor</title>
                                                        <meta http-equiv='refresh' content='3' />
                                                    </head>
                                                    <body>
                                                        <p><font size='3'>Time:{0}</font></p>
                                                        <br/>
                                                        <p><font size='6'>Luminosity: {1} lux</font></p>
                                                        <br />
                                                    </body>
                                                  </html>",

                                                DateTime.Now.ToString("h:mm:ss tt"),
                                                String.Format("{0:0.00}", CurrentLux));
                
                string htmlCode = "200 OK";

                using (Stream resp = outstream.AsStreamForWrite())
                {
                    byte[] bodyArray = Encoding.UTF8.GetBytes(respBody);
                    MemoryStream stream = new MemoryStream(bodyArray);

                    // Response heeader
                    string header = string.Format("HTTP/1.1 {0}\r\n" +
                                                  "Content-Type: text/html\r\n" + 
                                                  "Content-Length: {1}\r\n" +
                                                  "Connection: close\r\n\r\n",
                                                  htmlCode, stream.Length);

                    byte[] headerArray = Encoding.UTF8.GetBytes(header);
                    await resp.WriteAsync(headerArray, 0, headerArray.Length);
                    await stream.CopyToAsync(resp);
                    await resp.FlushAsync();
                }
            }

            public void Dispose()
            {
                sock.Dispose();
            }
        }

    }
}