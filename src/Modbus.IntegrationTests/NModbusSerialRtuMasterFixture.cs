using System;
using System.IO.Ports;
using MbUnit.Framework;
using Modbus.Device;

namespace Modbus.IntegrationTests
{
	[TestFixture]
	public class NModbusSerialRtuMasterFixture
	{
		[Test, ExpectedException(typeof(TimeoutException))]
		public void NModbusRtuMaster_ReadTimeout()
		{
			using (SerialPort port = ModbusMasterFixture.CreateAndOpenSerialPort(ModbusMasterFixture.DefaultMasterSerialPortName))
			{
				IModbusSerialMaster master = ModbusSerialMaster.CreateRtu(port);
				master.ReadCoils(100, 1, 1);
			}
		}
	}
}