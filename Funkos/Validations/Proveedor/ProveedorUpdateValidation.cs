using FluentValidation;
using Funkos.Dto.Proveedor;
using Funkos.Repositories.Proveedor;
using MongoDB.Bson;

namespace Funkos.Validations;
public class ProveedorUpdateValidation : AbstractValidator<ProveedorUpdateDto>
{
    public ProveedorUpdateValidation(IProveedorRepository repo)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio")
            .MaximumLength(200)
            .WithMessage("El nombre no puede superar los 200 caracteres")
            .MustAsync(async (dto, name, context, cancellationToken) =>
                {
                    var id = (ObjectId)context.RootContextData["ProvId"];
                   return !await repo.ExistsByNameAsync(name, id, cancellationToken);
                })
            .WithMessage("Ya existe un proveedor con ese nombre");

        RuleFor(x => x.Descripcion)
            .NotEmpty()
            .WithMessage("La descripción es obligatoria")
            .MaximumLength(300)
            .WithMessage("La descripción no puede superar los 300 caracteres");
    }
}