using System.Collections.Generic;

namespace YAPA.Shared.Contracts
{
    public interface IFontService
    {
        Dictionary<string, string> GetAllFonts();
        string GetFontPath(string name);
    }
}
