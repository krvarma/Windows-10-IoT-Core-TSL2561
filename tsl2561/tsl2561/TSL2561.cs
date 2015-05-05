using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace tsl2561
{
    class TSL2561
    {
        // TSL Address Constants
        public const int TSL2561_ADDR_0 = 0x29;    // address with '0' shorted on board 
        public const int TSL2561_ADDR = 0x39;      // default address 
        public const int TSL2561_ADDR_1 = 0x49;    // address with '1' shorted on board 
        // TSL Commands
        private const int TSL2561_CMD = 0x80;
        private const int TSL2561_CMD_CLEAR = 0xC0;
        // TSL Registers
        private const int TSL2561_REG_CONTROL = 0x00;
        private const int TSL2561_REG_TIMING = 0x01;
        private const int TSL2561_REG_THRESH_L = 0x02;
        private const int TSL2561_REG_THRESH_H = 0x04;
        private const int TSL2561_REG_INTCTL = 0x06;
        private const int TSL2561_REG_ID = 0x0A;
        private const int TSL2561_REG_DATA_0 = 0x0C;
        private const int TSL2561_REG_DATA_1 = 0x0E;

        // I2C Device
        private I2cDevice I2C;

        public TSL2561(ref I2cDevice I2CDevice)
        {
            this.I2C = I2CDevice;
        }

        // TSL2561 Sensor Power up
        public void PowerUp()
        {
            write8(TSL2561_REG_CONTROL, 0x03);
        }

        // TSL2561 Sensor Power down
        public void PowerDown()
        {
            write8(TSL2561_REG_CONTROL, 0x00);
        }

        // Retrieve TSL ID
        public byte GetId()
        {
            return I2CRead8(TSL2561_REG_ID);
        }

        // Set TSL2561 Timing and return the MS
        public int SetTiming(Boolean gain, byte time)
        {
            int ms = 0;

            switch (time)
            {
                case 0: ms = 14; break;
                case 1: ms = 101; break;
                case 2: ms = 402; break;
                default: ms = 0; break;
            }

            int timing = I2CRead8(TSL2561_REG_TIMING);

            // Set gain (0 or 1) 
            if (gain)
                timing |= 0x10;
            else
                timing &= (~0x10);

            // Set integration time (0 to 3) 

            timing &= ~0x03;
            timing |= (time & 0x03);

            write8(TSL2561_REG_TIMING, (byte)timing);

            return ms;
        }

        // Get channel data
        public uint[] GetData()
        {
            uint[] Data = new uint[2];

            Data[0] = I2CRead16(TSL2561_REG_DATA_0);
            Data[1] = I2CRead16(TSL2561_REG_DATA_1);

            return Data;
        }

        // Calculate Lux
        public double GetLux(Boolean gain, uint ms, uint CH0, uint CH1)
        {
            double ratio, d0, d1;
            double lux = 0.0;

            // Determine if either sensor saturated (0xFFFF)
            // If so, abandon ship (calculation will not be accurate)
            if ((CH0 == 0xFFFF) || (CH1 == 0xFFFF))
            {
                lux = 0.0;

                return lux;
            }

            // Convert from unsigned integer to floating point
            d0 = CH0; d1 = CH1;

            // We will need the ratio for subsequent calculations
            ratio = d1 / d0;

            // Normalize for integration time
            d0 *= (402.0 / ms);
            d1 *= (402.0 / ms);

            // Normalize for gain
            if (!gain)
            {
                d0 *= 16;
                d1 *= 16;
            }

            // Determine lux per datasheet equations:

            if (ratio < 0.5)
            {
                lux = 0.0304 * d0 - 0.062 * d0 * Math.Pow(ratio, 1.4);
            }
            else if (ratio < 0.61)
            {
                lux = 0.0224 * d0 - 0.031 * d1;
            }
            else if (ratio < 0.80)
            {
                lux = 0.0128 * d0 - 0.0153 * d1;
            }
            else if (ratio < 1.30)
            {
                lux = 0.00146 * d0 - 0.00112 * d1;
            }
            else
            {
                lux = 0.0;
            }

            return lux;
        }

        // Write byte
        private void write8(byte addr, byte cmd)
        {
            byte[] Command = new byte[] { (byte)((addr) | TSL2561_CMD), cmd };

            I2C.Write(Command);
        }

        // Read byte
        private byte I2CRead8(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TSL2561_CMD) };
            byte[] data = new byte[1];

            I2C.WriteRead(aaddr, data);

            return data[0];
        }

        // Read integer
        private ushort I2CRead16(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TSL2561_CMD) };
            byte[] data = new byte[2];

            I2C.WriteRead(aaddr, data);

            return (ushort)((data[1] << 8) | (data[0]));
        }
    }
}
