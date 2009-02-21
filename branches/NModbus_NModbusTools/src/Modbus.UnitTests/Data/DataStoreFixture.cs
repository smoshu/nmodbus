using System;
using Modbus.Data;
using System.Linq;
using NUnit.Framework;
using System.Diagnostics;

namespace Modbus.UnitTests.Data
{
	[TestFixture]
	public class DataStoreFixture
	{
		[Test]
		public void ReadData()
		{
			ModbusDataCollection<ushort> slaveCol = new ModbusDataCollection<ushort>(0, 1, 2, 3, 4, 5, 6);
			RegisterCollection result = DataStore.ReadData<RegisterCollection, ushort>(new DataStore(), slaveCol, 1, 3);
			Assert.AreEqual(new ushort[] { 1, 2, 3 }, result.ToArray());
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ReadDataStartAddressTooLarge()
		{
			DataStore.ReadData<DiscreteCollection, bool>(new DataStore(), new ModbusDataCollection<bool>(), 3, 2);
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ReadDataCountTooLarge()
		{
			DataStore.ReadData<DiscreteCollection, bool>(new DataStore(), new ModbusDataCollection<bool>(true, false, true, true), 1, 5);
		}

		[Test]
		public void ReadDataStartAddressZero()
		{
			DataStore.ReadData<DiscreteCollection, bool>(new DataStore(), new ModbusDataCollection<bool>(true, false, true, true, true, true), 0, 5);
		}

		[Test]
		public void WriteDataSingle()
		{
			ModbusDataCollection<bool> destination = new ModbusDataCollection<bool>(true, true);
			DiscreteCollection newValues = new DiscreteCollection(false);
			DataStore.WriteData(new DataStore(), newValues, destination, 0);
			Assert.AreEqual(false, destination[1]);
		}

		[Test]
		public void WriteDataMultiple()
		{
			ModbusDataCollection<bool> destination = new ModbusDataCollection<bool>(false, false, false, false, false, false, true);
			DiscreteCollection newValues = new DiscreteCollection(true, true, true, true);
			DataStore.WriteData(new DataStore(), newValues, destination, 0);
			Assert.AreEqual(new bool[] { false, true, true, true, true, false, false, true }, destination.ToArray());
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void WriteDataTooLarge()
		{
			ModbusDataCollection<bool> slaveCol = new ModbusDataCollection<bool>(true);
			DiscreteCollection newValues = new DiscreteCollection(false, false);
			DataStore.WriteData(new DataStore(), newValues, slaveCol, 1);
		}

		[Test]
		public void WriteDataStartAddressZero()
		{
			DataStore.WriteData(new DataStore(), new DiscreteCollection(false), new ModbusDataCollection<bool>(true, true), 0);
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void WriteDataStartAddressTooLarge()
		{
			DataStore.WriteData(new DataStore(), new DiscreteCollection(true), new ModbusDataCollection<bool>(true), 2);
		}

		/// <summary>
		/// http://modbus.org/docs/Modbus_Application_Protocol_V1_1b.pdf
		/// In the PDU Coils are addressed starting at zero. Therefore coils numbered 1-16 are addressed as 0-15.
		/// So reading Modbus address 0 should get you array index 1 in the DataStore.
		/// This implies that the DataStore array index 0 can never be used.
		/// </summary>
		[Test]
		public void TestReadMapping()
		{
			DataStore dataStore = DataStoreFactory.CreateDefaultDataStore();
			dataStore.HoldingRegisters.Insert(1, 45);
			dataStore.HoldingRegisters.Insert(2, 42);

			Assert.AreEqual(45, DataStore.ReadData<RegisterCollection, ushort>(dataStore, dataStore.HoldingRegisters, 0, 1)[0]);
			Assert.AreEqual(42, DataStore.ReadData<RegisterCollection, ushort>(dataStore, dataStore.HoldingRegisters, 1, 1)[0]);
		}

		[Test]
		public void DataStoreReadFromEvent_ReadHoldingRegisters()
		{
			DataStore dataStore = DataStoreFactory.CreateTestDataStore();

			bool readFromEventFired = false;
			bool writtenToEventFired = false;
			
			dataStore.DataStoreReadFrom += (obj, e) =>
			{
				readFromEventFired = true;
				Assert.AreEqual(3, e.StartAddress);
				Assert.AreEqual(new ushort[] { 4, 5, 6 }, e.Data.B.ToArray());
				Assert.AreEqual(ModbusDataType.HoldingRegister, e.ModbusDataType);
			};

			dataStore.DataStoreWrittenTo += (obj, e) => writtenToEventFired = true;

			DataStore.ReadData<RegisterCollection, ushort>(dataStore, dataStore.HoldingRegisters, 3, 3);
			Assert.IsTrue(readFromEventFired);
			Assert.IsFalse(writtenToEventFired);
		}

		[Test]
		public void DataStoreReadFromEvent_ReadInputRegisters()
		{
			DataStore dataStore = DataStoreFactory.CreateTestDataStore();

			bool readFromEventFired = false;
			bool writtenToEventFired = false;

			dataStore.DataStoreReadFrom += (obj, e) =>
			{
				readFromEventFired = true;
				Assert.AreEqual(4, e.StartAddress);
				Assert.AreEqual(new ushort[] { }, e.Data.B.ToArray());
				Assert.AreEqual(ModbusDataType.InputRegister, e.ModbusDataType);
			};

			dataStore.DataStoreWrittenTo += (obj, e) => writtenToEventFired = true;

			DataStore.ReadData<RegisterCollection, ushort>(dataStore, dataStore.InputRegisters, 4, 0);
			Assert.IsTrue(readFromEventFired);
			Assert.IsFalse(writtenToEventFired);
		}

		[Test]
		public void DataStoreReadFromEvent_ReadInputs()
		{
			DataStore dataStore = DataStoreFactory.CreateTestDataStore();

			bool readFromEventFired = false;
			bool writtenToEventFired = false;

			dataStore.DataStoreReadFrom += (obj, e) =>
			{
				readFromEventFired = true;
				Assert.AreEqual(4, e.StartAddress);
				Assert.AreEqual(new bool[] { false }, e.Data.A.ToArray());
				Assert.AreEqual(ModbusDataType.Input, e.ModbusDataType);
			};

			dataStore.DataStoreWrittenTo += (obj, e) => writtenToEventFired = true;

			DataStore.ReadData<DiscreteCollection, bool>(dataStore, dataStore.InputDiscretes, 4, 1);
			Assert.IsTrue(readFromEventFired);
			Assert.IsFalse(writtenToEventFired);
		}

		[Test]
		public void DataStoreWrittenToEvent_WriteCoils()
		{
			DataStore dataStore = DataStoreFactory.CreateTestDataStore();

			bool readFromEventFired = false;
			bool writtenToEventFired = false;

			dataStore.DataStoreWrittenTo += (obj, e) =>
			{
				writtenToEventFired = true;
				Assert.AreEqual(4, e.StartAddress);
				Assert.AreEqual(new bool[] { true, false, true }, e.Data.A.ToArray());
				Assert.AreEqual(ModbusDataType.Coil, e.ModbusDataType);
			};

			dataStore.DataStoreReadFrom += (obj, e) => readFromEventFired = true;

			DataStore.WriteData(dataStore, new DiscreteCollection(true, false, true), dataStore.CoilDiscretes, 4);
			Assert.IsFalse(readFromEventFired);
			Assert.IsTrue(writtenToEventFired);
		}

		[Test]
		public void DataStoreWrittenToEvent_WriteHoldingRegisters()
		{
			DataStore dataStore = DataStoreFactory.CreateTestDataStore();

			bool readFromEventFired = false;
			bool writtenToEventFired = false;

			dataStore.DataStoreWrittenTo += (obj, e) =>
			{
				writtenToEventFired = true;
				Assert.AreEqual(0, e.StartAddress);
				Assert.AreEqual(new ushort[] { 5, 6, 7 }, e.Data.B.ToArray());
				Assert.AreEqual(ModbusDataType.HoldingRegister, e.ModbusDataType);
			};

			dataStore.DataStoreReadFrom += (obj, e) => readFromEventFired = true;

			DataStore.WriteData(dataStore, new RegisterCollection(5, 6, 7), dataStore.HoldingRegisters, 0);
			Assert.IsFalse(readFromEventFired);
			Assert.IsTrue(writtenToEventFired);
		}
	}
}