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
        private static Random rand = new Random();
        public static string GenerateSession()
        {
            //All variables that were used are declared here.
            int[] randomChars = new int[9];
            int randomInt = 0;
            string firstSeq = "";
            string secondSeq = "";
            string fullSeq = "";

            //Loop until randomChars is full
            for (int i = 0; i < randomChars.Length;)
            {
                randomInt = rand.Next(65, 123);
                if (randomInt <= 90 || randomInt >= 97)
                {
                    randomChars[i] = randomInt;
                    i++;
                }
            }
            
            //Loop through randomChars and add them to firstSeq string
            foreach (int n in randomChars)
            {
                char character = (char)n;
                firstSeq = (firstSeq + character).ToString();
            }
           
            //Gets time stamp for second part of sequence
            secondSeq = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            //Combines first and second sequence
            fullSeq = (firstSeq + secondSeq);
            
            //Returns generated session as string
            return (fullSeq);
        }

    }
}