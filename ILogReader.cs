using System.Threading;
using System.Threading.Tasks;

namespace Wave.LogChallenge;

/// <summary>
/// – Write a program that will print out the total number of lines in the file.
/// – Notice that the 8th column contains a person’s name. Write a program that loads in this data and creates an array with all name strings.
/// - Print out the 432nd and 43243rd names.
/// – Notice that the 5th column contains a form of date. Count how many donations occurred in each month and print out the results.
/// – Notice that the 8th column contains a person’s name. Create an array with each first name. Identify the most common first name in
///   the data and how many times it occurs.
/// </summary>
public interface ILogReader
{
    Task<(long lines, string firstName, string secoundName, string eachMonth, string commonName)> ReadAsync(CancellationToken token = default);
}