/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

public class OnlineMapsDescriptionAttribute : Attribute
{
    private string name;

    public string Description
    {
        get { return name; }
    }

    public OnlineMapsDescriptionAttribute(string name)
    {
        this.name = name;
    }
}