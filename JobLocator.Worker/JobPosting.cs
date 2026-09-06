public class JobPosting // Класс для представления вакансии
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Organization? HiringOrganization { get; set; } // Связь с организацией, которая нанимает

    public JobLocation? JobLocation { get; set; }

    public Salary? BaseSalary { get; set; }

    public string? Url { get; set; }
}

public class Organization // Класс для представления организации, которая нанимает
{
    public string? Name { get; set; }
}

// Тут находятся два класса потому что в json ld данные о местоположении и адресе выглядят как два разных объекта и их нужно определять отдельно
public class JobLocation // Класс для представления местоположения вакансии
{
    public Address? Address { get; set; }
}

public class Address // Класс для представления адреса вакансии
{
    public string? AddressLocality { get; set; }
}

public class Salary
{
    public string? Currency { get; set; }
    public SalaryValue? Value { get; set; }
}

public class SalaryValue
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public string? UnitText { get; set; }
}