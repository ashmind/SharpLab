using System;
public class C
{
    public static void Main()
    {
        try
        {
            throw new Exception(); // [exception: Exception]
        }
        finally
        {
        } // [exception: Exception]
    }
}