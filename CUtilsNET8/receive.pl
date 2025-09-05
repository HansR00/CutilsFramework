#!/usr/bin/perl
# Project: CumulusUtils
# Date:    22 april 2021
# Purpose: CGI procedure to receive MapsOn files through HttpPost in the MapsOn function of Cutils
#
use warnings;
use CGI;
$CGI::POST_MAX = 1024*10;

local ($buffer, @pairs, $pair, $name, $value);

$filename='';
$filecontent='';

# Read in text
$ENV{'REQUEST_METHOD'} =~ tr/a-z/A-Z/;
if ($ENV{'REQUEST_METHOD'} eq "POST") {
   read(STDIN, $buffer, $ENV{'CONTENT_LENGTH'});
} else {
   $buffer = $ENV{'QUERY_STRING'};
}

# Split information into name/value pairs
@pairs = split(/&/, $buffer);
foreach $pair (@pairs) {
  ($name, $value) = split(/#/, $pair);

  if ($name eq "filename") {
    $filename = $value;
  } elsif ($name eq "filecontent") {
    $filecontent = $value;
  }
}

if ($filename and $filecontent) {
  open(FH, '>', "../maps/$filename") or die $!;
  print FH $filecontent;
  close(FH);
} else {
  $buffer = "CGI No Show...";
}

print CGI::header();
print "$buffer";

exit 1;
