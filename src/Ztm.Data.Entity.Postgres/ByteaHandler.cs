using System;
using System.Text;
using NBitcoin;
using Npgsql;
using Npgsql.BackendMessages;
using Npgsql.TypeHandling;

namespace Ztm.Data.Entity.Postgres
{
    class ByteaHandler :  NpgsqlSimpleTypeHandlerWithPsv<byte[], uint256>
    {
        internal readonly bool storeUInt256AsLE;

        public ByteaHandler(bool lendian = false)
        {
            storeUInt256AsLE = lendian;
        }

        #region Read
        public override byte[] Read(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription = null)
        {
            try
            {
                var bytes = new byte[len];
                buf.ReadBytes(bytes, 0, len);
                return bytes;
            }
            catch (Exception e)
            {
                throw new NpgsqlSafeReadException(e);
            }
        }

        protected override uint256 ReadPsv(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription = null)
        {
            try
            {
                var bytes = new byte[len];
                buf.ReadBytes(bytes, 0, len);
                return new uint256(bytes, storeUInt256AsLE);
            }
            catch (Exception e)
            {
                throw new NpgsqlSafeReadException(e);
            }
        }

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(byte[] value, NpgsqlParameter parameter)
            => value.Length;

        public override int ValidateAndGetLength(uint256 value, NpgsqlParameter parameter)
            => 32;

        public override void Write(uint256 value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
            => buf.WriteBytes(value.ToBytes(storeUInt256AsLE));

        public override void Write(byte[] value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
            => buf.WriteBytesRaw(value, false);

        #endregion Write
    }
}