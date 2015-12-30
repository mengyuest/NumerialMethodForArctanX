using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcTanX;

namespace TestDebugPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
//            String [] operStr = new string[4];
//            operStr[0] = "+";
//            operStr[1] = "-";
//operStr[2] = "*";
//operStr[3] = "/";

//            MyDecimal.SetRadixPoint(14);
//            MyDecimal a = new MyDecimal();
//            MyDecimal b= new MyDecimal();
//            MyDecimal c = new MyDecimal();

//            Random rd = new Random();

//            decimal A = new decimal ();
//            decimal B = new decimal();
//            decimal C = new decimal();

//            for (var i = 0; i <= 1000000; i++)
//            {
//                double fA = (rd.NextDouble() - 0.5)*1000;
//                double fB = (rd.NextDouble() - 0.5)*1000;
//                double fC = 0;

//                fA = double.Parse(String.Format("{0:F14}", fA));
//                fB = double.Parse(String.Format("{0:F14}", fB));

//                A = (decimal)(fA);
//                B = (decimal)(fB);

//                a = fA;
//                b = fB;

//                int choose = (rd.Next()%4);
//                switch (choose)
//                {
//                    case 0:
//                        C = A+B;
//                c = a+b;
//                fC = fA+fB;
//                        break;
//                    case 1:
//                         C = A-B;
//                c = a-b;
//                fC = fA-fB;
//                        break;
//                    case 2:
//                        C = A * B;
//                        c = a * b;
//                        fC = fA * fB;
//                        break;
//                    case 3:
//                        C = A / B;
//                        c = a / b;
//                        fC = fA / fB;
//                        break;

//                }
                
//                var strDecimal = String.Format("{0:F14}", C);
//                var strMynumbe = String.Format("{0}", c);
//                String strjudge = "";
//                if (strDecimal.CompareTo(strMynumbe) == 0)
//                {
//                    strjudge = "correct~";
//                    strjudge += Environment.NewLine + String.Format("{0} {1} {2}", a, operStr[choose], b);
//                    strjudge += Environment.NewLine + "  " + strDecimal + "  " + strMynumbe;
//                }
//                else
//                {
//                    strjudge = "@@@wrong!!!";
                   
//                    strjudge +=  Environment.NewLine + String.Format("{0} {1} {2}", a,operStr[choose],b);
//                    strjudge += Environment.NewLine + "decimal:" + String.Format("{0:F15}", C) + "  " + String.Format("{0:F14}", C);
//                    strjudge += Environment.NewLine + "mynum:" + strMynumbe;
//                    var str = c.RawNumber;
//                    Console.WriteLine(strjudge);
//                }

                
//            }

            MyDecimal.SetRadixPoint(7);
            int expectedPrecision = 3;
            String AStr = "";
            String BStr = "";
            MyDecimal A= new MyDecimal();
            MyDecimal B= new MyDecimal();

            for (var i = 0; i <= 10; i++)
            {
                AStr=Console.ReadLine();
                BStr=Console.ReadLine();
                A.RawNumber = AStr;
                B.RawNumber = BStr;
                bool Judge = MyDecimal.IsEqualAlmostly(A, B, expectedPrecision);
                Console.WriteLine(Judge);
            }
        }
    }
}
