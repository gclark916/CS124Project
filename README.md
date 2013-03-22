CS124Project
============

Short read DNA sequencing

Usage
=====
Actions
-------
argument | action 
--- | --- 
R, reference | generates usable text reference genome from raw text reference genome
D, donor | generates text donor genome from text reference genome 
s, shortreads | generates short reads from text donor genome
f, forward | generates precomputed data structures for reference genome
r, reverse | generates precomputed data structures for reverse reference genome
a, align | aligns reads, creates alignment database
c, construct | constructs output genome from alignment database 
A, accuracy | calculates accuracy of output genome vs donor genome 

Options
-------
argument | option | default
--- | --- | ---
basefile | set the base file name | default
rawref | set the raw reference file name | defaultraw.dna
coverage | set the coverage for generating short reads | 1
readlimit | limit the number of reads when generatign short reads | Unlimited
readlength | set the short read length when generating short reads | 30

Examples
--------

Generate everything from raw reference genome "hg19raw.dna." Set the base file name to "hg19". Use coverage of 5. Align reads, construct genome, compute accuracy:  
-RDsfracA -rawref=hg19raw.dna -basefile=hg19 -coverage=5

Generate 1,000,000 reads from donor genome, using base file name "hg19". hg19_donor.dna must exist:  
-s -basefile=hg19 -coverage=10 -limit=1000000

Files
-----
file | description
--- | ---
rawref | Text DNA file  
[basefile].dna: | Usuable reference genome. Compared to rawref, acgt are capitalized, all 'N' are replaced with one of ACGT at random, all other characters are removed  
[basefile].dna.bin: | Binary representation of reference genome  
[basefile].c: | Cumulative count precomputed data structure  
[basefile].occ | Compressed occurrence arrays  
[basefile].csa | Compressed suffix array  
[basefile]_output.dna | constructed text genome. May have 'N' if there were no reads covering that position  
[basefile].db | alignment database  

\*_rev.\*: reverse versions. Used in the MinDiff array calculation
