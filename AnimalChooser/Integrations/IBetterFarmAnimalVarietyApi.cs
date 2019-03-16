using System.Collections.Generic;

namespace AnimalChooser.Integrations
{
    public interface IBetterFarmAnimalVarietyApi
    {
        /// <summary>Get all farm animal categories that have been loaded.</summary>
        /// <returns>Returns Dictionary<string, List<string>></returns>
        Dictionary<string, List<string>> GetFarmAnimalCategories();
    }
}
