using RegistryService.Domain.Enums;
using RegistryService.Domain.Exceptions;

namespace RegistryService.Domain.ValueObjects;

public class Document
{
    public string Value { get; private set; }

    protected Document()
    {
        Value = null!;
    }

    public Document(string document, PersonType personType)
    {
        if (string.IsNullOrWhiteSpace(document))
            throw new DomainException("Documento é obrigatório.");

        document = OnlyNumbers(document);

        if (personType == PersonType.Individual)
        {
            if (!IsValidCpf(document))
                throw new DomainException("CPF inválido.");
        }
        else if (personType == PersonType.Company)
        {
            if (!IsValidCnpj(document))
                throw new DomainException("CNPJ inválido.");
        }
        else
        {
            throw new DomainException("Tipo de pessoa inválido.");
        }

        Value = document;
    }

    private static string OnlyNumbers(string input) =>
        new(input.Where(char.IsDigit).ToArray());

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var numbers = cpf.Select(c => c - '0').ToArray();

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += numbers[i] * (10 - i);

        var result = sum % 11;
        var digit1 = result < 2 ? 0 : 11 - result;

        if (numbers[9] != digit1)
            return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += numbers[i] * (11 - i);

        result = sum % 11;
        var digit2 = result < 2 ? 0 : 11 - result;

        return numbers[10] == digit2;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;

        var numbers = cnpj.Select(c => c - '0').ToArray();

        int[] weight1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weight2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += numbers[i] * weight1[i];

        var result = sum % 11;
        var digit1 = result < 2 ? 0 : 11 - result;

        if (numbers[12] != digit1)
            return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += numbers[i] * weight2[i];

        result = sum % 11;
        var digit2 = result < 2 ? 0 : 11 - result;

        return numbers[13] == digit2;
    }

    public override string ToString() => Value;
}

