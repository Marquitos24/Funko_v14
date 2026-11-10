using Funkos.Dto;
using Funkos.Dto.Funko;
using Funkos.Models;
using Microsoft.EntityFrameworkCore;

namespace Funkos.Mappers;

public static class FunkoMapper
{
    public static Funko ToEntityCreate(this FunkoCreateDto request) => new Funko
    {
        Name = request.Name,
        Precio = request.Precio,
        CategoriaId = request.CategoriaId,
        ProveedorId = request.ProveedorId,
        Stock =  request.Stock
        
    };
    
    public static Funko ToEntityUpdate(this FunkoUpdateDto request) => new Funko
    {
        Id = request.Id,
        Name = request.Name,
        Precio = request.Precio,
        CategoriaId = request.CategoriaId,
        ProveedorId = request.ProveedorId,
        Stock =  request.Stock
    };


    public static FunkosDtoResponse ToDtoResponse(this Funko funko) => new FunkosDtoResponse(
        funko.Id,
        funko.Name,
        funko.Precio,
        funko.CategoriaId,
        funko.ProveedorId,
        funko.Stock,
        funko.Imagen,
        funko.CreateAt,
        funko.UpdateAt,
        funko.DeleteAt
    );
    
    public static List<FunkosDtoResponse> ToDtoList(this IEnumerable<Funko> funkos) =>
        funkos.Select(f => f.ToDtoResponse()).ToList();

    
    public static FunkoParaCategoriaDto ToDtoParaCategoria(this Funko funko) => new FunkoParaCategoriaDto(
        funko.Id,
        funko.Name,
        funko.Precio
    );
}