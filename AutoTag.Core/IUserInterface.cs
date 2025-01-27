namespace AutoTag.Core;

public interface IUserInterface
{
    void DisplayMessage(string message, MessageType type);
    
    /// <summary>
    /// Update the path displayed for the current file
    /// </summary>
    /// <param name="path">New path for current file</param>
    void SetFilePath(string path);
    
    /// <summary>
    /// Set a status message for the current file
    /// </summary>
    /// <param name="status">Status message</param>
    /// <param name="type">Message type</param>
    void SetStatus(string status, MessageType type);
    
    /// <summary>
    /// Set a status message for the current file as a result of an exception
    /// </summary>
    /// <param name="status">Status message</param>
    /// <param name="type">Message type</param>
    /// <param name="ex">Throw exception</param>
    void SetStatus(string status, MessageType type, Exception ex);
    
    /// <summary>
    /// Select an option from a list of options
    /// </summary>
    /// <param name="message">Message to display alongside options</param>
    /// <param name="options">Options</param>
    /// <returns>Index of selected option, or <see langword="null"/> if none selected</returns>
    int? SelectOption(string message, List<string> options);
}