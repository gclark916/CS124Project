using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/* prefix trie tells you the indices of where a prefix can begin, given the character at beginning of prefix
 * 
 * so first iteration, prefix "begins" with character read[29]
 *      second iteration, prefix begins with character read[28]
 *        third iteration, prefix begins with with character read[27]
 * 
 * take one of the edges that the trie has for your character
 * 
 * An edge exists in the trie only if there exists a prefix that starts with that character somewhere
 * in the reference genome. The number of leaves is the number of unique prefixes
 * 
 * if the next node doesn't have an out edge for the next character you are looking for (read[28]),
 * then just assume read[28] is one of the mismatches and try the first edge that exists.
 * repeat until read is aligned. if ever there would need to be more than two mismatches, jump back to
 * whereever the last mismatch was and try the next available edge. If all available edges have been
 * exhausted for that mismatched position, then jump back to first mismatch.
 * 
 * insertions are handled by skipping a character in the read.
 * deletions are handled by skipping a character in the tree (this means that for the character previous 
 * to the "deleted" one, take all outgoing edges without iterating read)
 * */
namespace CS124Project.Genome
{
    class Searcher
    {
    }
}
