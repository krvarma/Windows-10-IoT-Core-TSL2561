Luminosity Sensor Sample using Windows IoT Core
----------------------------------------

As you all know Microsoft released Insider Preview of Windows 10 IoT Core recently. I too download and installed it on my RPi2. While playing with Windows 10 IoT Core, I decided to work on TSL2561 Luminosity Sensor using I2C. Here the sample I created this weekend.

In this project I am using TSL2561 Luminosity Sensor connected to RPi2 using I2C. This project is based on [Sparkfun TSL2561 Library](https://github.com/sparkfun/TSL2561_Luminosity_Sensor_BOB). This library is ported to Windows 10 IoT Core. This was simple and a straight forward posting. The sample read the luminosity and display it. You can either connect RPi2 to a monitor or access the internal HTTP server running on port 8080. The HTTP Server code is shamelessly copied from the super awesome project **[Hands-on-lab IoT Weather Station using Windows 10](http://www.hackster.io/windows-iot-maker/build-hands-on-lab-iot-weather-station-using-windows-10)**. 

**Wiring**

 - TSL2561 3V3 to RPi2 3.3v (Pin #01) 
 - TSL2561 GND to RPi2 Ground (Pin #09) 
 - TSL2561 SDA to RPi2 SDA (GPIO02) 
 - TSL2561 SCL to RPi2 SCL (GPIO03)

**Using the sample**
Open the TSL256 solution in VS2015. Start the application using the 'Remote Device' option under the Debug Tab. Configure the IP Address of the RPi2. 

To see the sensor values, either connect the RPi2 to a monitor or access the internal HTTP Server using the URL **`http://<<RPi2 IP Address>>:8080`**. The web page automatically refresh every 3 seconds.

**Screenshots**

![Wiring](https://raw.githubusercontent.com/krvarma/Windows-10-IoT-Core-TSL2561/master/images/W10-TSL2561_bb.png)

![Schematic](https://raw.githubusercontent.com/krvarma/Windows-10-IoT-Core-TSL2561/master/images/W10-TSL2561_schem.png)

![RPi2](https://raw.githubusercontent.com/krvarma/Windows-10-IoT-Core-TSL2561/master/images/IMG_0024.JPG)

![Output](https://raw.githubusercontent.com/krvarma/Windows-10-IoT-Core-TSL2561/master/images/browser.png)