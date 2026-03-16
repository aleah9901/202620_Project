/*
 * This is the SessionService class
 * In this class, a 24-char session name will be randomly generated.
 * This method will be called from the QTI_Editor.WWW.Services namespace, Session service class
 * It will return the session as a string
 * The first 9 characters will be randomly generated and the trailing characters will be the date (yyyyMMdd_HHmmss)
 */
using System;

namespace QTI_Editor.WWW.Services
{
    public class SessionService
    {
        //Creates an instance of the Random class, which will allow us to use the rant.Next function
        //ref:https://learn.microsoft.com/en-us/dotnet/api/system.random?view=net-10.0
        private static Random rand = new Random();
        public static string GenerateSession()
        {
            //All variables that were used are declared here.
            int[] randomChars = new int[9];
            int randomInt = 0;
            string firstSeq = "";
            string secondSeq = "";
            string fullSeq = "";

            /*This loop will will fill the randomChars array with integers.
             * It will generate a random interger from the range of 65-123
             * Then a test will happen if the integer is a value that has a char ASCII code
             * If not a char ASCII, it will return to the loop to get another integer.
             * Loop will stop when 9 characters have been selected
             */
            for (int i = 0; i < randomChars.Length;)
            {
                //rand.Next will choose a random integer in the range
                //ref:https://learn.microsoft.com/en-us/dotnet/api/system.random.next?view=net-10.0
                randomInt = rand.Next(65, 123);
                if (randomInt <= 90 || randomInt >= 97)
                {
                    randomChars[i] = randomInt;
                    i++;
                }
            }
            /* 
             * This loop will convert the integers in the randomChars array to char characters
             * Then will add the characters to the firstSeq
             */
            foreach (int n in randomChars)
            {
                char character = (char)n;
                firstSeq = (firstSeq + character).ToString();
            }
            /*
             * secondSeq will hold the current date and time
             * Uses the DateTime.Now property from DateTime Struct in System namespace
             * ref:https://learn.microsoft.com/en-us/dotnet/api/system.datetime.now?view=net-10.0
             */
            secondSeq = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            fullSeq = (firstSeq + secondSeq);
            /*
             * This method will return a string of a generated 24-char session name
             * Will be used to name a cache-directory
             * QTI_Editor.WWW\cache\(generatod session name).zip
             */
            return (fullSeq);
        }

    }
}