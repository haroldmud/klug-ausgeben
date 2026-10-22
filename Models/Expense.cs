using System.ComponentModel.DataAnnotations;

namespace klug_ausgeben.Models;

public class Expense
{
  public int Id { get; set; }
  [Required]
  public decimal Value { get; set; }

  [Required]
  public string? Description { get; set; }
}
