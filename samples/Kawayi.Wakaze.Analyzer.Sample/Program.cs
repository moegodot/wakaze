using Kawayi.Wakaze.Abstractions;

var validFamily = new SchemaFamily("semantic://wakaze.dev/tag");
var invalidFamily = new SchemaFamily("semantic://wakaze.dev");

var validSchema = new SchemaId("semantic://wakaze.dev/tag/v1");
var invalidSchema = new SchemaId("semantic://wakaze.dev/tag/v0");

Console.WriteLine(validFamily);
Console.WriteLine(invalidFamily);
Console.WriteLine(validSchema);
Console.WriteLine(invalidSchema);
