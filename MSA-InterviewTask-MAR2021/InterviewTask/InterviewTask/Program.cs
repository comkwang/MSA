using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;



namespace Datetimetest
{
    class Program
    {

        public const string FilePath = @"qbank-export-1-11-2020.txt"; // file name location

        const int GapTime = 5; // sec gap to determin duplicated data

        public class FinancialDataItem
        {
            public string Date { get; set; }
            public string Time { get; set; }
            public double Debit { get; set; }
            public double Credit { get; set; }
            public string PaidTo { get; set; }
            public string PaidBy { get; set; }
            public string Description { get; set; }


        }
      
        public class GroupedPayment
        {
            public double Payment { get; set; }
            public int Count { get; set; }
            public string PaidTo {get; set;}
        }

  
        static int Converttointtime(string input)
        {

            string[] time = input.Split(':');
            return  Convert.ToInt32(time[0]) * 3600 + Convert.ToInt32(time[1]) * 60 + Convert.ToInt32(time[2]);          
        }
        static void Main(string[] args)
        {
            //Testtime();
           

            if (File.Exists(FilePath))
            {

                List<FinancialDataItem> data = ImportFinancialData(FilePath);

                Console.WriteLine("--------------------------------");


                List<FinancialDataItem> possibleDuplicates = GetPossibleDuplicates(data);
                Console.WriteLine("--------------------------------");


                List<GroupedPayment> groupedPayments = GetGroupedExpenditures(data);



                Console.WriteLine("--------------------------------");
            } else {
                Console.WriteLine("File not exist");
            }
            Console.ReadKey();

        }
        
        //Task 1 - 
        public static List<FinancialDataItem> ImportFinancialData(string inputPath)
        {

            try
            {               
                string[] lines = File.ReadAllLines(inputPath);
                Console.WriteLine("TASK 1:");
               
                var financialData = new List<FinancialDataItem>();            


                foreach (var line in lines.Skip(1))
                {
                   
                    string[] splitData = line.Split('\t');
                    if (splitData[1] == "") splitData[1] = "0";
                    if (splitData[2] == "") splitData[2] = "0";
                    financialData.Add(
                      new FinancialDataItem
                      {
                         
                          Date = splitData[0].Substring(0, 10),
                          Time = splitData[0].Substring(11),
                          Debit = Double.Parse(splitData[1], NumberStyles.AllowCurrencySymbol | NumberStyles.AllowThousands | NumberStyles.Any),
                          Credit = Double.Parse(splitData[2], NumberStyles.AllowCurrencySymbol | NumberStyles.AllowThousands | NumberStyles.Any),
                          PaidTo = splitData[3],
                          PaidBy = splitData[4],
                          Description = splitData[5]
                      });
                 
                }

                var data = (from row in financialData
                            where row.Credit > 0
                            select row.Credit).Sum();
                Console.WriteLine(string.Format("-CREDITS - TOTAL: {0:C}", data));

                var data1 = (from row in financialData
                            where row.Credit > 0                        
                            select new { row.Date, row.Time, row.Credit, row.PaidBy, row.Description } );

                foreach (var abc in data1)
                     Console.WriteLine("-- {0} {1} : RECEIVED {2} from {3}-  {4}",abc.Date, abc.Time, abc.Credit, abc.PaidBy, abc.Description);

                    var data2 = (from row in financialData
                             where row.Debit > 0
                             select row.Debit).Sum();            
                Console.WriteLine(string.Format("-DEBITS - TOTAL: -{0:C}", data2));

                var data3 = (from row in financialData
                             where row.Debit > 0
                             select new { row.Date, row.Time, row.Debit, row.PaidTo} );

                    
                foreach (var abc in data3)            

                Console.WriteLine("-- {0} {1} : PAID {2:C} to {3}", abc.Date, abc.Time, abc.Debit, abc.PaidTo);
                return financialData;
            }
            catch( Exception e)
            {
                Console.WriteLine($"The source file was not match\n{e}");
                return null;
            }
        }
        //Task 2 - 
        public static List<FinancialDataItem> GetPossibleDuplicates(List<FinancialDataItem> data)
        {
            List<FinancialDataItem> possibleDuplicates = new List<FinancialDataItem>();
           

            possibleDuplicates = data;
            Console.WriteLine("TASK 2:");
          

            var a1 = from o in possibleDuplicates                     
                     group o by new { o.Date, o.PaidTo } into g
                     where g.Count() == 2
                     select new { g.Key.Date, g.Key.PaidTo };
            
            
            var a2 = from o in possibleDuplicates
                     from g in a1
                     where o.Date == g.Date && o.PaidTo == g.PaidTo
                     select new { o.Date, o.Time, o.PaidTo };
          
            var a3 = from o in possibleDuplicates
                     from g in a2                 
                     where o.Date == g.Date && o.PaidTo == g.PaidTo && o.Time !=  g.Time  
                     select new { o.Date, o.Time, next = g.Time, o.Debit, o.PaidTo };               
           
            var a4 = from o in a3
                     where Math.Abs( Converttointtime(o.Time)- Converttointtime(o.next)) <= GapTime
                     select new { o.Date, o.Time, o.next, o.Debit, o.PaidTo };
            Console.WriteLine("- Possible Duplicates");
            foreach (var abc in a4)
                Console.WriteLine("-- {0} {1} : PAID {2} to {3}", abc.Date, abc.Time, abc.Debit, abc.PaidTo);
                   

            return possibleDuplicates;

        }
        //Task 3 - 
        public static List<GroupedPayment> GetGroupedExpenditures(List<FinancialDataItem> data)
        {
            List<GroupedPayment> groupedPayments = new List<GroupedPayment>();
           
            Console.WriteLine("TASK 3:");

            var dataset = (from r in data
                           group r by r.PaidTo into g
                           select new { Payment = g.Sum(r => r.Debit), Count = g.Count(), PaidTo = g.Key }).OrderByDescending(o => o.Payment);
           
            int rank = 1;
            Console.WriteLine("- Grouped Payment");
            foreach (var row in dataset)
                groupedPayments.Add(
                    new GroupedPayment
                    {
                        Payment = row.Payment,
                        Count = row.Count,
                        PaidTo = row.PaidTo                      
                    });

            foreach (var row in groupedPayments)
            {
                if (row.Payment <= 0) continue;
                Console.WriteLine("-- {0}: PAID {1:C} in {2} {3} to {4}", rank++, row.Payment, row.Count, (row.Count > 1) ? "Payments" : "Payment", row.PaidTo);
            }

            return groupedPayments;
        }
        public void test()
        {
            Console.WriteLine("test");
        }

    }
}
