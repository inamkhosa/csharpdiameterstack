using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiameterStack;
using System.Net;
using System.Configuration;
using System.Threading;
using DiameterStack.Common;

namespace Samples
{
    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: December, 2013
    /// </summary>
    /// 
    class ClientMain

    {

        private static int iTaskCnt=0;
        private static int iActiveTasks = 0;
        static void Main(string[] args)
        {

            try
            {

                DiameterClient evcClient = new DiameterClient();

                //evcClient.stackContext = DiameterAAAStack.Initialize();


                /**Test Case#1 CHECK_BALANCE*/
                //for (int i = 0; i < 100; i++)
                //{

                //clsServiceResult sr = evcClient.fnGetSubscriberInfo("617066600");

                //clsServiceResult sr = evcClient.fnRechargeSubscriber("312132132", "617066600", 100, 10);
                //GPRS Recharge
                // clsServiceResult sr = evcClient.fnPromoteSubscriber("312132142", 4, "617066600", 5, 900);
                //Bundle Recharge
                //clsServiceResult sr = evcClient.fnBundleRechargeSubscriber("312132142","617066600", "4");
                //Display Service Result
                //foreach (clsServiceParam sp in sr.mData)
                //  Console.WriteLine("Param = " + sp.pName + ", Value=" + sp.pValue);


                ClientMain MainClient = new ClientMain();
                TimeSpan JobStart = new TimeSpan(DateTime.Now.Ticks);

                MainClient.StartClient();
                Console.WriteLine("Active Tasks:" + iActiveTasks);     
                while(iActiveTasks !=0)
                    Console.WriteLine("Active Tasks:" + iActiveTasks);
                TimeSpan JobEnd = new TimeSpan(DateTime.Now.Ticks);

                // DiameterStack.Common.StackLog.Write2ErrorLog("Main","\n\nJob took:" + JobEnd.Subtract(JobStart).TotalSeconds + " seconds to complete");
                Console.WriteLine("\n\nJob took:" + JobEnd.Subtract(JobStart).TotalSeconds + " seconds to complete");
                Console.ReadLine();

            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.ToString());

                //Console.ReadLine();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartClient()
        {
            DiameterClient Evc = new DiameterClient();

            Evc.stackContext = DiameterAAAStack.Initialize();
            int mobnum = 615551110;

            for (int x = 0; x < 10; x++)
            {
                for (int i = 0; i < 20; i++)
                {
                    Thread t = new Thread(() => DoWork(Evc,mobnum.ToString()));
                    t.Start();
                    mobnum++;
                    iTaskCnt++;
                    //DoWork(Evc);
                }
                Thread.Sleep(1000);
            }
        }

        public void DoWork(object state,string mobileno)
        {
            Interlocked.Increment(ref iActiveTasks);
            DiameterClient clnt = (DiameterClient)state;
            ServiceResult sr = clnt.fnGetSubscriberInfo(mobileno);
            Interlocked.Decrement(ref iActiveTasks);
            //Display Service Result
            //foreach (clsServiceParam sp in sr.mData)
            //    Console.WriteLine("Param = " + sp.pName + ", Value=" + sp.pValue);
        }
        
       


    }
}
