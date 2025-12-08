namespace Demo;
//For CS Inc.
public static class StringHelper
{


    public static IEnumerable<string> ReturnSubString(string input)
    {
        IEnumerable<string> result = new List<string>();



        for (int str = 0; str < input.Length; str++)
        {

            for (int final = str; final < input.Length; final++)
            {
                yield return input.Substring(str, final - str + 1);
            }


        }

    }

}