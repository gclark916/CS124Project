CS124Project
============

Short read DNA sequencing

Usage
=====
Actions
-------
argument | action
--- | ---
R, reference | generates text reference genome from raw text reference genome
D, donor | generates text donor genome from text reference genome
s, shortreads | generates short reads from text donor genome
f, forward | generates precomputed data structures for reference genome
r, reverse | generates precomputed data structures for reverse reference genome
a, align | aligns reads, creates alignment database
c, construct | constructs output genome from alignment database
A, accuracy | calculates accuracy of output genome vs donor genome

Options
-------
argument | option
--- | ---
basefile | set the base file name. Default is "default"
rawref | set the raw reference file name. Default is "defaultraw.dna"
coverage | set the coverage for generating short reads. Default is 1
readlimit | limit the number of reads when generatign short reads
readlength | set the short read length when generating short reads. Default is 30

Examples
--------

Generate everything from raw reference genome "hg19.dna." Set the base file name to "hg19". Use coverage of 5. Align reads, construct genome, compute accuracy:
-RDsfracA -rawref=hg19.dna -basefile=hg19 -coverage=5

Generate 1,000,000 reads from donor genome:
-s -coverage=10 -limit=1000000