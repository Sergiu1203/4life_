namespace _4Life.Data
{
    // Lista predefinita de medicamente folosita in dropdown-uri
    // Doctorul si pacientul pot alege din aceasta lista sau pot scrie manual
    public static class MedicineDatabase
    {
        public static List<MedicineSuggestion> All { get; } = new()
        {
            // Analgezice / Antipiretice
            new("Paracetamol",      "Analgesic",        "500mg"),
            new("Ibuprofen",        "Analgesic",        "400mg"),
            new("Aspirin",          "Analgesic",        "500mg"),
            new("Naproxen",         "Analgesic",        "250mg"),
            new("Metamizol",        "Analgesic",        "500mg"),

            // Antibiotice
            new("Amoxicillin",      "Antibiotic",       "500mg"),
            new("Augmentin",        "Antibiotic",       "875mg"),
            new("Azithromycin",     "Antibiotic",       "500mg"),
            new("Ciprofloxacin",    "Antibiotic",       "500mg"),
            new("Doxycycline",      "Antibiotic",       "100mg"),
            new("Clarithromycin",   "Antibiotic",       "500mg"),

            // Cardiovasculare
            new("Amlodipine",       "Cardiovascular",   "5mg"),
            new("Bisoprolol",       "Cardiovascular",   "5mg"),
            new("Lisinopril",       "Cardiovascular",   "10mg"),
            new("Atorvastatin",     "Cardiovascular",   "20mg"),
            new("Metoprolol",       "Cardiovascular",   "50mg"),
            new("Ramipril",         "Cardiovascular",   "5mg"),
            new("Warfarin",         "Cardiovascular",   "5mg"),

            // Diabet
            new("Metformin",        "Diabetes",         "500mg"),
            new("Insulin Glargine", "Diabetes",         "10 units"),
            new("Sitagliptin",      "Diabetes",         "100mg"),
            new("Glibenclamide",    "Diabetes",         "5mg"),

            // Gastro
            new("Omeprazole",       "Gastrointestinal", "20mg"),
            new("Pantoprazole",     "Gastrointestinal", "40mg"),
            new("Domperidone",      "Gastrointestinal", "10mg"),
            new("Loperamide",       "Gastrointestinal", "2mg"),
            new("Ranitidine",       "Gastrointestinal", "150mg"),

            // Respirator
            new("Salbutamol",       "Respiratory",      "100mcg"),
            new("Montelukast",      "Respiratory",      "10mg"),
            new("Cetirizine",       "Respiratory",      "10mg"),
            new("Loratadine",       "Respiratory",      "10mg"),
            new("Fluticasone",      "Respiratory",      "50mcg"),
            new("Ambroxol",         "Respiratory",      "30mg"),

            // Neurologie / Psihiatrie
            new("Sertraline",       "Psychiatry",       "50mg"),
            new("Escitalopram",     "Psychiatry",       "10mg"),
            new("Alprazolam",       "Psychiatry",       "0.25mg"),
            new("Melatonin",        "Psychiatry",       "3mg"),
            new("Diazepam",         "Psychiatry",       "5mg"),

            // Vitamine / Suplimente
            new("Vitamin C",        "Supplement",       "500mg"),
            new("Vitamin D3",       "Supplement",       "1000 IU"),
            new("Vitamin B12",      "Supplement",       "1000mcg"),
            new("Magnesium",        "Supplement",       "375mg"),
            new("Zinc",             "Supplement",       "10mg"),
            new("Iron",             "Supplement",       "14mg"),
            new("Omega-3",          "Supplement",       "1000mg"),
            new("Folic Acid",       "Supplement",       "400mcg"),

            // Altele
            new("Prednisone",       "Corticosteroid",   "5mg"),
            new("Dexamethasone",    "Corticosteroid",   "4mg"),
            new("Furosemide",       "Diuretic",         "40mg"),
            new("Levothyroxine",    "Thyroid",          "50mcg"),
        };

        // Grupate pe categorii pentru Picker
        public static List<string> Categories { get; } =
            All.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();

        public static List<string> Names { get; } =
            All.Select(m => m.Name).OrderBy(n => n).ToList();

        public static MedicineSuggestion? FindByName(string name) =>
            All.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public class MedicineSuggestion
    {
        public string Name            { get; }
        public string Category        { get; }
        public string SuggestedDosage { get; }

        public MedicineSuggestion(string name, string category, string dosage)
        {
            Name            = name;
            Category        = category;
            SuggestedDosage = dosage;
        }

        public override string ToString() => Name;
    }
}
