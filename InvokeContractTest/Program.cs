using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Globalization;
using ThinNeo;
using Zoro;
using Zoro.Cryptography;
using Zoro.Ledger;
using Zoro.Network.P2P.Payloads;
using Zoro.SmartContract;
using Zoro.Wallets;
using System.Threading;

namespace InvokeContractTest
{
    class Program
    {
        string api = "https://api.nel.group/api/testnet";
        string local = "http://127.0.0.1:20332/";

        string id_GAS = "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

        static string wif = "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3";
        static byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
        static byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
        public static string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
        Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

        public async Task publishContract()
        {
            byte[] script = System.IO.File.ReadAllBytes("BcpContract.avm");
            Console.WriteLine("合约脚本：" + ThinNeo.Helper.Bytes2HexString(script));
            Console.WriteLine("合约脚本Hash：" + ThinNeo.Helper.Bytes2HexString(ThinNeo.Helper.GetScriptHashFromScript(script).data.ToArray().Reverse().ToArray()));
            byte[] parameter__list = ThinNeo.Helper.HexString2Bytes("0710");
            byte[] return_type = ThinNeo.Helper.HexString2Bytes("05");
            int need_storage = 1;
            int need_nep4 = 0;
            int need_canCharge = 4;
            string name = "mygas";
            string version = "1.0";
            string auther = "LZ";
            string email = "0";
            string description = "0";
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                var ss = need_storage | need_nep4 | need_canCharge;
                sb.EmitPushString(description);
                sb.EmitPushString(email);
                sb.EmitPushString(auther);
                sb.EmitPushString(version);
                sb.EmitPushString(name);
                sb.EmitPushNumber(ss);
                sb.EmitPushBytes(return_type);
                sb.EmitPushBytes(parameter__list);
                sb.EmitPushBytes(script);
                sb.EmitSysCall("Zoro.Contract.Create");

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                postArray.AddArrayValue("0");
                postArray.AddArrayValue(scriptPublish);

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(local, "invokescript", out postdata, postArray.ToArray());
                var result = await Helper.HttpPost(url, postdata);
                //return;

                MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;

                var consume = json_result_obj["gas_consumed"].ToString();

                decimal gas_consumed = decimal.Parse(consume);

                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //extdata.gas = Math.Ceiling(gas_consumed);
                extdata.gas = 0;

                ThinNeo.Transaction tran = makeTran(null, null, new ThinNeo.Hash256(id_GAS), extdata.gas);
                tran.version = 1;
                tran.extdata = extdata;
                tran.type = ThinNeo.TransactionType.InvocationTransaction;

                //附加鉴证
                tran.attributes = new ThinNeo.Attribute[1];
                tran.attributes[0] = new ThinNeo.Attribute();
                tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
                tran.attributes[0].data = scripthash;

                byte[] msg = tran.GetMessage();
                byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                tran.AddWitness(signdata, pubkey, address);
                string txid = tran.GetHash().ToString();
                byte[] data = tran.GetRawData();
                string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                postRawArray.AddArrayValue("");
                postRawArray.AddArrayValue(rawdata);

                url = Helper.MakeRpcUrlPost(local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                result = await Helper.HttpPost(url, postdata);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        public static ThinNeo.Transaction makeTran(Dictionary<string, List<Utxo>> dir_utxos, string targetaddr, ThinNeo.Hash256 assetid, decimal sendcount)
        {
            var tran = new ThinNeo.Transaction();
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            tran.version = 0;
            tran.extdata = null;

            tran.attributes = new ThinNeo.Attribute[0];
            var scraddr = "";
            decimal count = decimal.Zero;
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            tran.inputs = list_inputs.ToArray();
            List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
            tran.outputs = list_outputs.ToArray();

            return tran;
        }

        public async Task ContractTransaction()
        {
            string wif = "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3";
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            var scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(int)1");
                sb.EmitParamJson(array);
                sb.EmitPushString("deploy");
                sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                postArray.AddArrayValue("0");
                postArray.AddArrayValue(scriptPublish);

                byte[] postdata;
                
                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //extdata.gas = Math.Ceiling(gas_consumed - 10);
                extdata.gas = 0;

                ThinNeo.Transaction tran = makeTran(null, null, new ThinNeo.Hash256(id_GAS), extdata.gas);
                tran.version = 1;
                tran.extdata = extdata;
                tran.type = ThinNeo.TransactionType.InvocationTransaction;

                //附加鉴证
                tran.attributes = new ThinNeo.Attribute[1];
                tran.attributes[0] = new ThinNeo.Attribute();
                tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
                tran.attributes[0].data = scripthash;

                byte[] msg = tran.GetMessage();
                byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                tran.AddWitness(signdata, pubkey, address);
                string txid = tran.GetHash().ToString();
                byte[] data = tran.GetRawData();
                string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                postRawArray.AddArrayValue("0");
                postRawArray.AddArrayValue(rawdata);

                var url = Helper.MakeRpcUrlPost(local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                var result = await Helper.HttpPost(url, postdata);
            }
        }

        public async Task Transfer() {
            string wif = "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3";
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            var scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            string targetAddress = ThinNeo.Helper.GetAddressFromPublicKey(ThinNeo.Helper.HexString2Bytes("0397f5990762e69fb90831e390ff1cec056a604e1f1fee69ee91433ef33e9bf254"));

            var i = 100000000;
            ThreadPool.QueueUserWorkItem(async (p)=> {
                while (true) {
                    i++;
                    using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
                    {
                        MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                        array.AddArrayValue("(addr)" + address);//from
                        array.AddArrayValue("(addr)" + targetAddress);//to
                        array.AddArrayValue("(int)" + i);//value
                        sb.EmitParamJson(array);
                        sb.EmitPushString("transfer");
                        sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

                        string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                        MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                        postArray.AddArrayValue("0");
                        postArray.AddArrayValue(scriptPublish);

                        ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                        extdata.script = sb.ToArray();

                        //extdata.gas = Math.Ceiling(gas_consumed - 10);
                        extdata.gas = 0;

                        ThinNeo.Transaction tran = makeTran(null, null, new ThinNeo.Hash256(id_GAS), extdata.gas);
                        tran.version = 1;
                        tran.extdata = extdata;
                        tran.type = ThinNeo.TransactionType.InvocationTransaction;

                        //附加鉴证
                        tran.attributes = new ThinNeo.Attribute[1];
                        tran.attributes[0] = new ThinNeo.Attribute();
                        tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
                        tran.attributes[0].data = scripthash;

                        byte[] msg = tran.GetMessage();
                        byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                        tran.AddWitness(signdata, pubkey, address);
                        string txid = tran.GetHash().ToString();
                        byte[] data = tran.GetRawData();
                        string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                        MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                        postRawArray.AddArrayValue("0");
                        postRawArray.AddArrayValue(rawdata);

                        byte[] postdata;
                        var url = Helper.MakeRpcUrlPost(local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                        var result = await Helper.HttpPost(url, postdata);
                        Console.WriteLine(address + " " + targetAddress + "  " + i);
                    }
                }              
            });            
        }

        public async void start()
        {
            //await publishContract();
            //await ContractTransaction();
            //await GetNep5Asset();
            await GetBanlanceOf();
            //await Transfer();
        }

        public async void publish()
        {
            await publishContract();
        }

        public async Task GetNep5Asset()
        {
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            sb.EmitParamJson(array);
            sb.EmitPushString("name");
            sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

            sb.EmitParamJson(array);
            sb.EmitPushString("totalSupply");
            sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

            sb.EmitParamJson(array);
            sb.EmitPushString("symbol");
            sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

            sb.EmitParamJson(array);
            sb.EmitPushString("decimals");
            sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue("0");
            postArray.AddArrayValue(scriptPublish);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(local, "invokescript", out postdata, postArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);

            //Console.WriteLine(result);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

            if (stack.Count == 4)
            {
                Console.WriteLine("name:" + GetJsonString(stack[0] as MyJson.JsonNode_Object));
                Console.WriteLine("totalSupply:" + GetJsonBigInteger(stack[1] as MyJson.JsonNode_Object));
                Console.WriteLine("symbol:" + GetJsonString(stack[2] as MyJson.JsonNode_Object));
                Console.WriteLine("decimals:" + GetJsonInteger(stack[3] as MyJson.JsonNode_Object));
            }
        }

        private string GetJsonString(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "ByteArray")
            {
                var bt = ThinNeo.Debug.DebugTool.HexString2Bytes(value.AsString());
                string str = System.Text.Encoding.ASCII.GetString(bt);
                return str;

            }
            return "";
        }

        private string GetJsonBigInteger(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "ByteArray")
            {
                var bt = ThinNeo.Debug.DebugTool.HexString2Bytes(value.AsString());
                var num = new BigInteger(bt);
                return num.ToString();

            }
            return "";
        }

        private string GetJsonInteger(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "Integer")
            {
                return value.ToString();

            }
            return "";
        }

        public async Task GetBanlanceOf()
        {
            string targetAddress = ThinNeo.Helper.GetAddressFromPublicKey(ThinNeo.Helper.HexString2Bytes("0397f5990762e69fb90831e390ff1cec056a604e1f1fee69ee91433ef33e9bf254"));
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + targetAddress);
            sb.EmitParamJson(array);
            sb.EmitPushString("balanceOf");
            sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue("0");
            postArray.AddArrayValue(scriptPublish);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(local, "invokescript", out postdata, postArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

            if (stack.Count == 1)
            {
                Console.WriteLine("balanceOf:" + GetJsonBigInteger(stack[0] as MyJson.JsonNode_Object));
            }
        }

        static void Main(string[] args)
        {
            //new Program().publish();
            new Program().start();
            //ManyThread.Init();
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }

    class ManyThread {
        public static int ThreadID = 0;

        static string wif = "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3";
        static string targetWif = "6PYPMmXc6p9JJLhPVpkubvmUnoKxqynxQ7RML1urcvmxwzDuH2GUo6JPb8";
        static byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
        static byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
        static string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
        static Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
        static string targetAddress = "AUJRntTMkwc4T6m4Ss4p8J42Np8Z9DH8u8";

        public static void Init() {
            for (int i = 0; i < 20; i++) {
                Task.Factory.StartNew(ThreadMethodAsync);
            }
        }

        public static void ThreadMethodAsync() {
            
            while (true)
            {
                lock (typeof(ManyThread)) {
                    ThreadID++;
                    using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
                    {
                        MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                        array.AddArrayValue("(addr)" + address);//from
                        array.AddArrayValue("(addr)" + targetAddress);//to
                        array.AddArrayValue("(int)" + ThreadID);//value
                        sb.EmitParamJson(array);
                        sb.EmitPushString("transfer");
                        sb.EmitAppCall(new Hash160("0xc4108917282bff79b156d4d01315df811790c0e8"));

                        string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                        MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                        postArray.AddArrayValue("0");
                        postArray.AddArrayValue(scriptPublish);

                        ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                        extdata.script = sb.ToArray();

                        //extdata.gas = Math.Ceiling(gas_consumed - 10);
                        extdata.gas = 0;

                        ThinNeo.Transaction tran = Program.makeTran(null, null, null, extdata.gas);
                        tran.version = 1;
                        tran.extdata = extdata;
                        tran.type = ThinNeo.TransactionType.InvocationTransaction;

                        //附加鉴证
                        tran.attributes = new ThinNeo.Attribute[1];
                        tran.attributes[0] = new ThinNeo.Attribute();
                        tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
                        tran.attributes[0].data = scripthash;

                        byte[] msg = tran.GetMessage();
                        byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                        tran.AddWitness(signdata, pubkey, address);
                        string txid = tran.GetHash().ToString();
                        byte[] data = tran.GetRawData();
                        string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                        MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                        postRawArray.AddArrayValue("0");
                        postRawArray.AddArrayValue(rawdata);

                        byte[] postdata;
                        var url = Helper.MakeRpcUrlPost("http://127.0.0.1:20332/", "sendrawtransaction", out postdata, postRawArray.ToArray());
                        var result = Helper.HttpPost(url, postdata);
                        Console.WriteLine(address + " " + targetAddress + "  " + ThreadID);
                    }
                }                
            }
        }
    }
}
