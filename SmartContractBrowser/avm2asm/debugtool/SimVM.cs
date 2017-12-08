using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Debug
{
    public class State
    {
        public int StateID
        {
            get;
            private set;
        }
        public void SetId(int id)
        {
            this.StateID = id;
        }
        public Neo.VM.RandomAccessStack<string> ExeStack = new VM.RandomAccessStack<string>();
        public Neo.VM.RandomAccessStack<Neo.SmartContract.Debug.StackItem> CalcStack = new VM.RandomAccessStack<SmartContract.Debug.StackItem>();
        public Neo.VM.RandomAccessStack<Neo.SmartContract.Debug.StackItem> AltStack = new VM.RandomAccessStack<SmartContract.Debug.StackItem>();
        public void PushExe(string hash)
        {
            ExeStack.Push(hash);
            StateID++;
        }
        public void PopExe()
        {
            ExeStack.Pop();
            StateID++;
        }
        public bool CalcCalcStack(VM.OpCode op)
        {
            if (op == VM.OpCode.TOALTSTACK)
            {
                var p = CalcStack.Pop();
                AltStack.Push(p);
                StateID++;
                return true;
            }
            if (op == VM.OpCode.FROMALTSTACK)
            {
                var p = AltStack.Pop();
                CalcStack.Push(p);
                StateID++;
                return true;
            }
            if (op == VM.OpCode.XSWAP)
            {
                var item = CalcStack.Pop();
                var xn = CalcStack.Peek(item.AsInt());
                var swapv = CalcStack.Peek(0);
                CalcStack.Set(item.AsInt(), swapv);
                CalcStack.Set(0, xn);
                StateID++;
                return true;
            }
            return false;
        }
        public void CalcCalcStack(SmartContract.Debug.Op stackop, SmartContract.Debug.StackItem item)
        {
            if (stackop.type == SmartContract.Debug.OpType.Push)
            {
                CalcStack.Push(item);
            }
            else if (stackop.type == SmartContract.Debug.OpType.Insert)
            {
                CalcStack.Insert(stackop.ind, item);
            }
            else if (stackop.type == SmartContract.Debug.OpType.Clear)
            {
                CalcStack.Clear();
            }
            else if (stackop.type == SmartContract.Debug.OpType.Set)
            {
                CalcStack.Set(stackop.ind, item);
            }
            else if (stackop.type == SmartContract.Debug.OpType.Pop)
            {
                CalcStack.Pop();
            }
            else if (stackop.type == SmartContract.Debug.OpType.Peek)
            {
                //CalcStack.Peek(stackop.ind);
            }
            else if (stackop.type == SmartContract.Debug.OpType.Remove)
            {
                CalcStack.Remove(stackop.ind);
            }
            if(stackop.type!= SmartContract.Debug.OpType.Peek)//peek 不造成状态变化
                StateID++;
        }
        public void DoSysCall()
        {

        }
        public State Clone()
        {
            State state = new State();
            state.StateID = this.StateID;
            foreach (var s in ExeStack)
            {
                state.ExeStack.Push(s);
            }
            foreach (var s in CalcStack)
            {
                state.CalcStack.Push(s.Clone());
            }
            foreach (var s in AltStack)
            {
                state.AltStack.Push(s.Clone());
            }
            return state;
        }
    }
    //模拟虚拟机
    public class SimVM
    {
        public void Execute(SmartContract.Debug.FullLog FullLog)
        {
            State runstate = new State();
            runstate.SetId(0);

            stateClone = new Dictionary<int, State>();
            mapState = new Dictionary<SmartContract.Debug.LogOp, int>();
            ExecuteScript(runstate, FullLog.script);
        }
        public Dictionary<int, State> stateClone;
        public Dictionary<SmartContract.Debug.LogOp, int> mapState;
        void ExecuteScript(State runstate, SmartContract.Debug.LogScript script)
        {
            runstate.PushExe(script.hash);
            foreach (var op in script.ops)
            {
                if (op.op == VM.OpCode.APPCALL)//不造成栈影响，由目标script影响
                {
                    ExecuteScript(runstate, op.subScript);
                    mapState[op] = runstate.StateID;
                }
                else if (op.op == VM.OpCode.CALL)//不造成栈影响 就是个jmp
                {
                    runstate.PushExe(script.hash);
                    mapState[op] = runstate.StateID;
                }
                else if (op.op == VM.OpCode.RET)
                {
                    mapState[op] = runstate.StateID;
                }
                else
                {
                    if(op.op== VM.OpCode.SYSCALL)//syscall比较独特，有些syscall 可以产生独立的log
                    {
                        //runstate.DoSysCall(op.op);
                    }
                    if (runstate.CalcCalcStack(op.op) == false)
                    {
                        if (op.stack != null)
                        {

                            for (var i = 0; i < op.stack.Length; i++)
                            {
                                if (i == op.stack.Length - 1)
                                {
                                    runstate.CalcCalcStack(op.stack[i], op.opresult);
                                }
                                else
                                {
                                    runstate.CalcCalcStack(op.stack[i], null);
                                }
                            }
                        }
                    }
                    if (stateClone.ContainsKey(runstate.StateID) == false)
                    {
                        stateClone[runstate.StateID] = (Neo.Debug.State)runstate.Clone();
                    }
                    mapState[op] = runstate.StateID;
                }
            }
        }
    }
}
