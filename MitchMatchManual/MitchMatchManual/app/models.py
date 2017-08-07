"""
Definition of models.
"""

from django.db import models

class Record(models.Model):
    EnterpriseId = models.IntegerField()
    LastName = models.TextField(max_length=128)
    FirstName = models.TextField(max_length = 128)
    MiddleName = models.TextField(max_length= 128)
    Suffix = models.TextField(max_length=64)
    DOB = models.TextField(max_length=128)
    Gender = models.TextField(max_length = 8)
    SSN = models.TextField(max_length=64)
    Address1 = models.TextField()
    Address2 = models.TextField()
    Zip = models.TextField(64)
    MothersMaidenName = models.TextField(128)
    MRN = models.TextField(64)
    City = models.TextField(128)
    State = models.TextField(64)
    Phone = models.TextField(64)
    Phone2 = models.TextField(64)
    Email = models.TextField(128)
    Alias = models.TextField(128)

class Set(models.Model):
    Checked = models.BooleanField()
    Notes = models.TextField()

class SetMember(models.Model):
    RecordId = models.ForeignKey(Record,  on_delete=models.CASCADE)
    SetId = models.ForeignKey(Set, on_delete=models.CASCADE)
    IsGood = models.NullBooleanField()


