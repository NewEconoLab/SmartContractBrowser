using System;
using System.Collections.Generic;
using Neo.VM;
namespace Neo.vmext
{
    public class Transaction : IScriptContainer
    {
        public Transaction()
        {
            this.Inputs.Add(new TransactionInput());
            this.Outputs.Add(new TransactionOutput());
        }
        public byte[] GetMessage()
        {
            return new byte[] { 1 };
            // throw new NotImplementedException();
        }

        public byte[] ToArray()
        {
            throw new NotImplementedException();
        }
        public List<TransactionInput> Inputs = new List<TransactionInput>();
        public List<TransactionOutput> Outputs = new List<TransactionOutput>();
    }
    public class TransactionInput : IInteropInterface
    {
        public byte[] PrevHash
        {
            get
            {
                return new byte[] { 1, 23, 44, 44 };
            }
        }

        public ushort PrevIndex
        {
            get
            {
                return 7;
            }
        }
        public byte[] ToArray()
        {
            throw new NotImplementedException();
        }

    }
    public class TransactionOutput : IInteropInterface
    {
        public byte[] PrevHash
        {
            get
            {
                return new byte[] { 1, 23, 44, 44 };
            }
        }

        public ushort PrevIndex
        {
            get
            {
                return 7;
            }
        }
        public byte[] ToArray()
        {
            throw new NotImplementedException();
        }


    }
    public class Block : IScriptContainer
    {
        public Block()
        {
            //this.Inputs.Add(new TransactionInput());
            //this.Outputs.Add(new TransactionOutput());
        }
        public byte[] GetMessage()
        {
            return new byte[] { 1 };
            // throw new NotImplementedException();
        }

        public byte[] ToArray()
        {
            throw new NotImplementedException();
        }
        public List<TransactionInput> Inputs = new List<TransactionInput>();
        public List<TransactionOutput> Outputs = new List<TransactionOutput>();
    }
    public class testapiservice : Neo.VM.InteropService
    {
        public testapiservice()
        {
            Register("AntShares.Transaction.GetInputs", Transaction_GetInputs);
            Register("AntShares.Input.GetHash", Input_GetHash);
            Register("AntShares.Input.GetIndex", Input_GetIndex);
            Register("AntShares.Output.GetIndex", Output_GetIndex);
            Register("AntShares.Output.GetHash", Output_GetHash);
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("AntShares.Storage.GetContext", Storage_GetContext);
            Register("AntShares.Storage.Get", Storage_Get);
            Register("AntShares.Storage.CurrentContext", Storage_GetContext);
            Register("AntShares.Storage.Delete", Storage_Delete);

            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetIndex", Output_GetIndex);
            Register("Neo.Output.GetHash", Output_GetHash);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("Neo.Storage.Put", Storage_Put);
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("Neo.Storage.CurrentContext", Storage_GetContext);
            Register("Neo.Storage.Delete", Storage_Delete);

            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("Neo.Runtime.Log", Runtime_Log);

            Register("Neo.Block.GetTransaction", Block_GetTransaction);
            Register("Neo.Runtime.GetTrigger", Runtime_GetTrigger);
        }
        //easy add for test
        protected virtual bool Runtime_Notify(ExecutionEngine engine)
        {
            var array = engine.EvaluationStack.Pop().GetArray();
            var s1 = System.Text.Encoding.UTF8.GetString(array[0].GetByteArray());
            //var s2 = System.Text.Encoding.UTF8.GetString(array[1].GetByteArray());
            return true;
        }
        protected virtual bool Runtime_Log(ExecutionEngine engine)
        {
            var str = engine.EvaluationStack.Pop().GetString();
            Console.WriteLine("log:" + str);
            return true;
        }
        protected virtual bool Runtime_GetTrigger(ExecutionEngine engine)
        {
            var br = System.Windows.Forms.MessageBox.Show("选择入口", "yes =  0x00 no ==0x10 cancel==0x11", System.Windows.Forms.MessageBoxButtons.YesNoCancel);
            StackItem item = null;
            if (br == System.Windows.Forms.DialogResult.Yes)
            {
                item = 0;

            }
            if (br == System.Windows.Forms.DialogResult.No)
            {
                item = 0x10;

            }
            if (br == System.Windows.Forms.DialogResult.Cancel)
            {
                item = 0x11;

            }
            engine.EvaluationStack.Push(item);
            return true;
        }
        private static bool Transaction_GetInputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;

            List<StackItem> array = new List<StackItem>();

            for (int i = 0; i < tx.Inputs.Count; i++)
            {
                array.Add(StackItem.FromInterface(tx.Inputs[i]));
            }
            StackItem _array = array.ToArray();

            engine.EvaluationStack.Push(_array);


            return true;
        }
        private static bool Input_GetHash(ExecutionEngine engine)
        {
            var input = engine.EvaluationStack.Pop().GetInterface<TransactionInput>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash);
            return true;
        }
        private static bool Output_GetHash(ExecutionEngine engine)
        {
            var v = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (v == null) return false;
            engine.EvaluationStack.Push(v.PrevHash);
            return true;
        }
        private static bool Input_GetIndex(ExecutionEngine engine)
        {
            var input = engine.EvaluationStack.Pop().GetInterface<TransactionInput>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }
        private static bool Output_GetIndex(ExecutionEngine engine)
        {
            var v = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (v == null) return false;
            engine.EvaluationStack.Push((int)v.PrevIndex);
            return true;
        }
        private static bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push((int)365);
            return true;
        }
        private static bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            var b = engine.EvaluationStack.Pop().GetByteArray();
            string text = System.Text.Encoding.UTF8.GetString(b);
            //var v = engine.EvaluationStack.Pop().GetBigInteger();

            engine.EvaluationStack.Push(StackItem.FromInterface(new Block()));
            return true;
        }
        private static bool Block_GetTransaction(ExecutionEngine engine)
        {
            var block = engine.EvaluationStack.Pop().GetInterface<Block>();
            var i = (int)engine.EvaluationStack.Pop().GetBigInteger();
            engine.EvaluationStack.Push(StackItem.FromInterface(block.Outputs[0]));
            return true;
        }

        private static bool Header_GetTimestamp(ExecutionEngine engine)
        {
            var b = engine.EvaluationStack.Pop().GetInterface<Block>();

            engine.EvaluationStack.Push((uint)3655);
            return true;
        }
        static byte[] storage;
        private static bool Storage_Put(ExecutionEngine engine)
        {
            var value = engine.EvaluationStack.Pop().GetByteArray();
            var key = engine.EvaluationStack.Pop().GetByteArray();
            var pos = engine.EvaluationStack.Pop().GetByteArray();
            storage = value;
            //engine.EvaluationStack.Push((uint)3655);
            return true;
        }
        private static bool Storage_Get(ExecutionEngine engine)
        {
            var key = engine.EvaluationStack.Pop().GetByteArray();
            var pos = engine.EvaluationStack.Pop().GetByteArray();

            engine.EvaluationStack.Push((uint)3655);
            return true;
        }
        private static bool Storage_Delete(ExecutionEngine engine)
        {
            var key = engine.EvaluationStack.Pop().GetBigInteger();
            var pos = engine.EvaluationStack.Pop().GetBigInteger();
            return true;
        }
        private static bool Storage_GetContext(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push((uint)3655);

            //engine.EvaluationStack.Push((uint)3655);
            return true;
        }
    }
}
