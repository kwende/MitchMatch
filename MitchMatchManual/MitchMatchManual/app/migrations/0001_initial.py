# -*- coding: utf-8 -*-
# Generated by Django 1.11.4 on 2017-08-07 03:25
from __future__ import unicode_literals

from django.db import migrations, models
import django.db.models.deletion


class Migration(migrations.Migration):

    initial = True

    dependencies = [
    ]

    operations = [
        migrations.CreateModel(
            name='Record',
            fields=[
                ('id', models.AutoField(auto_created=True, primary_key=True, serialize=False, verbose_name='ID')),
                ('EnterpriseId', models.IntegerField()),
                ('LastName', models.TextField(max_length=128)),
                ('FirstName', models.TextField(max_length=128)),
                ('MiddleName', models.TextField(max_length=128)),
                ('Suffix', models.TextField(max_length=64)),
                ('DOB', models.TextField(max_length=128)),
                ('Gender', models.TextField(max_length=8)),
                ('SSN', models.TextField(max_length=64)),
                ('Address1', models.TextField()),
                ('Address2', models.TextField()),
                ('Zip', models.TextField(verbose_name=64)),
                ('MothersMaidenName', models.TextField(verbose_name=128)),
                ('MRN', models.TextField(verbose_name=64)),
                ('City', models.TextField(verbose_name=128)),
                ('State', models.TextField(verbose_name=64)),
                ('Phone', models.TextField(verbose_name=64)),
                ('Phone2', models.TextField(verbose_name=64)),
                ('Email', models.TextField(verbose_name=128)),
                ('Alias', models.TextField(verbose_name=128)),
            ],
        ),
        migrations.CreateModel(
            name='Set',
            fields=[
                ('id', models.AutoField(auto_created=True, primary_key=True, serialize=False, verbose_name='ID')),
                ('Checked', models.BooleanField()),
                ('Notes', models.TextField()),
            ],
        ),
        migrations.CreateModel(
            name='SetMember',
            fields=[
                ('id', models.AutoField(auto_created=True, primary_key=True, serialize=False, verbose_name='ID')),
                ('IsGood', models.NullBooleanField()),
                ('RecordId', models.ForeignKey(on_delete=django.db.models.deletion.CASCADE, to='app.Record')),
                ('SetId', models.ForeignKey(on_delete=django.db.models.deletion.CASCADE, to='app.Set')),
            ],
        ),
    ]
