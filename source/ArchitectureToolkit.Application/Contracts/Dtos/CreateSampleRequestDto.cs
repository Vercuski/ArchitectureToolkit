using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Contracts.Dtos;

public sealed record CreateSampleRequestDto(
    string? DtoSampleString,
    bool DtoSampleBoolean,
    int DtoSampleInt,
    decimal DtoSampleDecimal
    ) : IDomainMapper<SampleEntityDefinition>
{
    public SampleEntityDefinition MapToDomain()
    {
        return new SampleEntityDefinition
        {
            SampleString = DtoSampleString,
            SampleBoolean = DtoSampleBoolean,
            SampleInt = DtoSampleInt,
            SampleDecimal = DtoSampleDecimal
        };
    }
}
