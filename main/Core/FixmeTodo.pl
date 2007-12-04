#!/usr/bin/perl -w
use strict;

my $results = "FixmeTodo.list";

# remove old run
system "rm $results";

print "Autogenerating list of TODO's and FIXME's\n";

my $cmd = 'find . -name \'*.cs\' > tmp.list';
system $cmd;

open LIST, "tmp.list";
chomp (my @list = <LIST>);

# ugly output
foreach my $source (@list) {
	my $grepcmd = "grep -n TODO $source >> $results";
	my $tmp = system $grepcmd;

	if ($tmp == 0)
	{
		system "echo \"end of $source\" >> $results";
	}

	$grepcmd = "grep -n FIXME $source >> $results";
	$tmp = system $grepcmd;

	if ($tmp == 0)
	{
		system "echo \"end of $source\" >> $results";
	}

}

# remove temp file
system "rm tmp.list";
print "done\n";

