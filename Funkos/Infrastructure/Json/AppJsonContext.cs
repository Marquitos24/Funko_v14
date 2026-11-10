using System.Text.Json.Serialization;
using Funkos.Dto;
using Funkos.Dto.Auth;
using Funkos.Dto.Categoria;
using Funkos.Dto.Common;
using Funkos.Dto.Funko;
using Funkos.Dto.Proveedor;
using Funkos.Models;

namespace Funkos.Infrastructure.Json;

/// <summary>
/// Contexto de serialización JSON con source-gen para AOT/trimming.
/// </summary>
[JsonSerializable(typeof(AuthCreateDtoRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(AuthDtoResponse))]
[JsonSerializable(typeof(CategoriaCreateDto))]
[JsonSerializable(typeof(CategoriaDtoResponse))]
[JsonSerializable(typeof(CategoriaUpdateDto))]
[JsonSerializable(typeof(CategoriaWithFunkosResponseDto))]
[JsonSerializable(typeof(List<CategoriaDtoResponse>))]
[JsonSerializable(typeof(List<FunkosDtoResponse>))]
[JsonSerializable(typeof(FunkoFilterDto))]
[JsonSerializable(typeof(FunkosDtoResponse))]
[JsonSerializable(typeof(FunkoCreateDto))]
[JsonSerializable(typeof(FunkoUpdateDto))]
[JsonSerializable(typeof(IEnumerable<ProveedorDtoResponse>))]
[JsonSerializable(typeof(ProveedorDtoResponse))]
[JsonSerializable(typeof(ProveedorCreateDto))]
[JsonSerializable(typeof(ProveedorUpdateDto))]
[JsonSerializable(typeof(IEnumerable<Funko>))]
[JsonSerializable(typeof(PagedResult<FunkosDtoResponse>))]

[JsonSerializable(typeof(Funko))]
[JsonSerializable(typeof(Proveedor))]
[JsonSerializable(typeof(Categoria))]
[JsonSerializable(typeof(Role))]
public partial class AppJsonContext : JsonSerializerContext;