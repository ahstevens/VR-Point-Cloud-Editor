/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Declares a type of control in the wizard.
/// </summary>
public class OnlineMapsWizardControlHelperAttribute : Attribute
{
    /// <summary>
    /// Result type
    /// </summary>
    public OnlineMapsTarget resultType;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="resultType">Result type</param>
    public OnlineMapsWizardControlHelperAttribute(OnlineMapsTarget resultType)
    {
        this.resultType = resultType;
    }
}