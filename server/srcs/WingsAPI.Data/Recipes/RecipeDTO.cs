// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PhoenixLib.DAL;

namespace WingsEmu.DTOs.Recipes;

public class RecipeDTO : IIntDto
{
    public int Amount { get; set; }
    public int? ProducerMapNpcId { get; set; }
    public int ProducedItemVnum { get; set; }
    public int? ProducerItemVnum { get; set; }
    public int? ProducerNpcVnum { get; set; }
    public List<RecipeItemDTO> Items { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
}