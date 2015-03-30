// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Metadata;
using Xunit;

// ReSharper disable UnusedMember.Local
// ReSharper disable ImplicitlyCapturedClosure

namespace Microsoft.Data.Entity.Tests.Metadata
{
    public class EntityTypeTest
    {
        private readonly Model _model = BuildModel();

        private class A
        {
            public string E { get; set; }
            public string G { get; set; }
        }

        private class B : A
        {
            public string F { get; set; }
            public string H { get; set; }
        }

        private class C : A
        {
        }

        private class D : C
        {
        }

        [Fact]
        public void Circular_inheritance_should_throw()
        {
            var model = new Model();

            //    A
            //   / \
            //  B   C
            //       \
            //        D

            var a = new EntityType(typeof(A), model);
            var b = new EntityType(typeof(B), model) { BaseType = a };
            var c = new EntityType(typeof(C), model) { BaseType = a };
            var d = new EntityType(typeof(D), model) { BaseType = c };

            Assert.Equal(
                Strings.CircularInheritance(a, a),
                Assert.Throws<InvalidOperationException>(() => a.BaseType = a).Message);

            Assert.Equal(
                Strings.CircularInheritance(a, b),
                Assert.Throws<InvalidOperationException>(() => a.BaseType = b).Message);

            Assert.Equal(
                Strings.CircularInheritance(a, d),
                Assert.Throws<InvalidOperationException>(() => a.BaseType = d).Message);
        }

        [Fact]
        public void Properties_on_base_type_should_be_inherited()
        {
            var model = new Model();

            //    A
            //   / \
            //  B   C

            var a = new EntityType(typeof(A), model);
            a.AddProperty("G", typeof(string));
            a.AddProperty("E", typeof(string));

            var b = new EntityType(typeof(B), model);
            b.AddProperty("H", typeof(string));
            b.AddProperty("F", typeof(string));

            var c = new EntityType(typeof(C), model);
            c.AddProperty("H", typeof(string), true);
            c.AddProperty("I", typeof(string), true);

            Assert.Equal(new[] { "E", "G" }, a.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "F", "H" }, b.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "H", "I" }, c.Properties.Select(p => p.Name).ToArray());

            b.BaseType = a;
            c.BaseType = a;

            Assert.Equal(new[] { "E", "G" }, a.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "E", "G", "F", "H" }, b.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "E", "G", "H", "I" }, c.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { 0, 1, 2, 3 }, b.Properties.Select(p => p.Index));
            Assert.Equal(new[] { 0, 1, 2, 3 }, c.Properties.Select(p => p.Index));
        }

        [Fact]
        public void Properties_added_to_base_type_should_be_inherited()
        {
            var model = new Model();

            //    A
            //   / \
            //  B   C

            var a = new EntityType(typeof(A), model);
            var b = new EntityType(typeof(B), model);
            var c = new EntityType(typeof(C), model);

            b.BaseType = a;
            c.BaseType = a;

            a.AddProperty("G", typeof(string));
            a.AddProperty("E", typeof(string));

            b.AddProperty("H", typeof(string));
            b.AddProperty("F", typeof(string));

            c.AddProperty("H", typeof(string), true);
            c.AddProperty("I", typeof(string), true);

            Assert.Equal(new[] { "E", "G" }, a.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "E", "G", "F", "H" }, b.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { "E", "G", "H", "I" }, c.Properties.Select(p => p.Name).ToArray());
            Assert.Equal(new[] { 0, 1, 2, 3 }, b.Properties.Select(p => p.Index));
            Assert.Equal(new[] { 0, 1, 2, 3 }, c.Properties.Select(p => p.Index));
        }

        [Fact]
        public void Adding_property_throws_when_parent_type_has_property_with_same_name()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);
            a.AddProperty("G", typeof(string));

            var b = new EntityType(typeof(B), model);
            b.BaseType = a;

            Assert.Equal(
                Strings.DuplicateProperty("G", typeof(B).FullName),
                Assert.Throws<InvalidOperationException>(() => b.AddProperty("G", typeof(string), true)).Message);
        }

        [Fact]
        public void Adding_property_throws_when_grandparent_type_has_property_with_same_name()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);
            a.AddProperty("G", typeof(string));

            var b = new EntityType(typeof(B), model);
            b.BaseType = a;

            var c = new EntityType(typeof(C), model);
            c.BaseType = b;

            Assert.Equal(
                Strings.DuplicateProperty("G", typeof(C).FullName),
                Assert.Throws<InvalidOperationException>(() => c.AddProperty("G", typeof(string))).Message);
        }

        [Fact]
        public void Adding_property_throws_when_child_type_has_property_with_same_name()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);

            var b = new EntityType(typeof(B), model);
            b.BaseType = a;

            b.AddProperty("G", typeof(string));

            Assert.Equal(
                Strings.DuplicateProperty("G", typeof(A).FullName),
                Assert.Throws<InvalidOperationException>(() => a.AddProperty("G", typeof(string))).Message);
        }

        [Fact]
        public void Adding_property_throws_when_grandchild_type_has_property_with_same_name()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);

            var b = new EntityType(typeof(B), model);
            b.BaseType = a;

            var c = new EntityType(typeof(C), model);
            c.BaseType = b;

            c.AddProperty("G", typeof(string));

            Assert.Equal(
                Strings.DuplicateProperty("G", typeof(A).FullName),
                Assert.Throws<InvalidOperationException>(() => a.AddProperty("G", typeof(string))).Message);
        }

        [Fact]
        public void Setting_base_type_throws_when_parent_contains_duplicate_property()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);
            a.AddProperty("G", typeof(string));

            var b = new EntityType(typeof(B), model);
            b.AddProperty("G", typeof(string), true);

            Assert.Equal(
                Strings.DuplicatePropertiesOnBase("G", typeof(B).FullName, typeof(A).FullName),
                Assert.Throws<InvalidOperationException>(() => b.BaseType = a).Message);
        }

        [Fact]
        public void Setting_base_type_throws_when_grandparent_contains_duplicate_property()
        {
            var model = new Model();

            var a = new EntityType(typeof(A), model);
            a.AddProperty("E", typeof(string));
            a.AddProperty("G", typeof(string));

            var b = new EntityType(typeof(B), model);
            b.BaseType = a;

            var c = new EntityType(typeof(C), model);
            c.AddProperty("E", typeof(string));
            c.AddProperty("G", typeof(string));

            Assert.Equal(
                Strings.DuplicatePropertiesOnBase("E, G", typeof(C).FullName, typeof(B).FullName),
                Assert.Throws<InvalidOperationException>(() => c.BaseType = b).Message);
        }

        [Fact]
        public void Can_create_entity_type()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(typeof(Customer).FullName, entityType.Name);
            Assert.Same(typeof(Customer), entityType.ClrType);
        }

        [Fact]
        public void Simple_name_is_simple_CLR_name()
        {
            Assert.Equal("EntityTypeTest", new EntityType(typeof(EntityTypeTest), new Model()).DisplayName());
            Assert.Equal("Customer", new EntityType(typeof(Customer), new Model()).DisplayName());
            Assert.Equal("List`1", new EntityType(typeof(List<Customer>), new Model()).DisplayName());
        }

        [Fact]
        public void Simple_name_is_part_of_name_following_final_separator_when_no_CLR_type()
        {
            Assert.Equal("Everything", new EntityType("Everything", new Model()).DisplayName());
            Assert.Equal("Is", new EntityType("Everything.Is", new Model()).DisplayName());
            Assert.Equal("Awesome", new EntityType("Everything.Is.Awesome", new Model()).DisplayName());
            Assert.Equal("WhenWe`reLivingOurDream", new EntityType("Everything.Is.Awesome+WhenWe`reLivingOurDream", new Model()).DisplayName());
        }

        [Fact]
        public void Can_set_reset_and_clear_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.SetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetPrimaryKey());

            Assert.Same(key1, entityType.FindPrimaryKey());
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.SetPrimaryKey(idProperty);

            Assert.NotNull(key2);
            Assert.Same(key2, entityType.GetPrimaryKey());
            Assert.Same(key2, entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);

            Assert.Same(key1, entityType.GetKey(key1.Properties));
            Assert.Same(key2, entityType.GetKey(key2.Properties));

            Assert.Null(entityType.SetPrimaryKey((Property)null));

            Assert.Null(entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);

            Assert.Null(entityType.SetPrimaryKey(new Property[0]));

            Assert.Null(entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);

            Assert.Equal(
                Strings.EntityRequiresKey(typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetPrimaryKey()).Message);
        }

        [Fact]
        public void Setting_primary_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.KeyPropertiesWrongEntity("{'" + Customer.IdProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.SetPrimaryKey(idProperty)).Message);
        }

        [Fact]
        public void Can_get_set_reset_and_clear_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty }));
            Assert.Same(key1, entityType.GetPrimaryKey());

            Assert.Same(key1, entityType.FindPrimaryKey());
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.GetOrSetPrimaryKey(idProperty);

            Assert.NotNull(key2);
            Assert.NotEqual(key1, key2);
            Assert.Same(key2, entityType.GetOrSetPrimaryKey(idProperty));
            Assert.Same(key2, entityType.GetPrimaryKey());
            Assert.Same(key2, entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);
            Assert.Same(key1, entityType.GetKey(key1.Properties));
            Assert.Same(key2, entityType.GetKey(key2.Properties));

            Assert.Null(entityType.GetOrSetPrimaryKey((Property)null));

            Assert.Null(entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);

            Assert.Null(entityType.GetOrSetPrimaryKey(new Property[0]));

            Assert.Null(entityType.FindPrimaryKey());
            Assert.Equal(2, entityType.Keys.Count);
            Assert.Equal(
                Strings.EntityRequiresKey(typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetPrimaryKey()).Message);
        }

        [Fact]
        public void Can_clear_the_primary_key_if_it_is_referenced_from_a_foreign_key()
        {
            var model = new Model();
            var entityType = model.AddEntityType(typeof(Customer));
            var idProperty = entityType.AddProperty(Customer.IdProperty);
            var customerPk = entityType.GetOrSetPrimaryKey(idProperty);

            var orderType = model.AddEntityType(typeof(Order));
            var fk = orderType.GetOrAddForeignKey(orderType.GetOrAddProperty(Order.CustomerIdProperty), customerPk);

            entityType.SetPrimaryKey((Property)null);

            Assert.Equal(1, entityType.Keys.Count);
            Assert.Same(customerPk, entityType.FindKey(idProperty));
            Assert.Null(entityType.FindPrimaryKey());
            Assert.Same(customerPk, fk.PrincipalKey);
        }

        [Fact]
        public void Can_change_the_primary_key_if_it_is_referenced_from_a_foreign_key()
        {
            var model = new Model();
            var entityType = model.AddEntityType(typeof(Customer));
            var idProperty = entityType.AddProperty(Customer.IdProperty);
            var customerPk = entityType.GetOrSetPrimaryKey(idProperty);

            var orderType = model.AddEntityType(typeof(Order));
            var fk = orderType.GetOrAddForeignKey(orderType.GetOrAddProperty(Order.CustomerIdProperty), customerPk);

            entityType.SetPrimaryKey(entityType.GetOrAddProperty(Customer.NameProperty));

            Assert.Equal(2, entityType.Keys.Count);
            Assert.Same(customerPk, entityType.FindKey(idProperty));
            Assert.NotSame(customerPk, entityType.GetPrimaryKey());
            Assert.Same(customerPk, fk.PrincipalKey);
        }

        [Fact]
        public void Can_add_and_get_a_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.AddKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetOrAddKey(new[] { idProperty, nameProperty }));
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.GetOrAddKey(idProperty);

            Assert.NotNull(key2);
            Assert.Same(key2, entityType.GetKey(idProperty));
            Assert.Equal(2, entityType.Keys.Count());
            Assert.Contains(key1, entityType.Keys);
            Assert.Contains(key2, entityType.Keys);
        }

        [Fact]
        public void Adding_a_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.KeyPropertiesWrongEntity("{'" + Customer.IdProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.AddKey(idProperty)).Message);
        }

        [Fact]
        public void Adding_a_key_throws_if_duplicated()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddKey(new[] { idProperty, nameProperty });

            Assert.Equal(
                Strings.DuplicateKey("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddKey(new[] { idProperty, nameProperty })).Message);
        }

        [Fact]
        public void Adding_a_key_throws_if_same_as_primary()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.Equal(
                Strings.DuplicateKey("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddKey(new[] { idProperty, nameProperty })).Message);
        }

        [Fact]
        public void Can_remove_keys()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(
                Strings.KeyNotFound("{'" + idProperty.Name + "', '" + nameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetKey(new[] { idProperty, nameProperty })).Message);
            Assert.Null(entityType.RemoveKey(new Key(new[] { idProperty })));

            var key1 = entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });
            var key2 = entityType.GetOrAddKey(idProperty);

            Assert.Equal(new[] { key2, key1 }, entityType.Keys.ToArray());

            Assert.Same(key1, entityType.RemoveKey(key1));
            Assert.Null(entityType.RemoveKey(key1));

            Assert.Equal(
                Strings.KeyNotFound("{'" + idProperty.Name + "', '" + nameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetKey(new[] { idProperty, nameProperty })).Message);

            Assert.Equal(new[] { key2 }, entityType.Keys.ToArray());

            Assert.Same(key2, entityType.RemoveKey(new Key(new[] { idProperty })));

            Assert.Empty(entityType.Keys);
        }

        [Fact]
        public void Removing_a_key_throws_if_it_referenced_from_a_foreign_key_in_the_model()
        {
            var model = new Model();

            var customerType = model.AddEntityType(typeof(Customer));
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerKey);

            Assert.Equal(
                Strings.KeyInUse("{'" + Customer.IdProperty.Name + "'}", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => customerType.RemoveKey(customerKey)).Message);
        }

        [Fact]
        public void Keys_are_ordered_by_property_count_then_property_names()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = customerType.GetOrAddProperty(Customer.NameProperty);
            var otherNameProperty = customerType.GetOrAddProperty("OtherNameProperty", typeof(string), shadowProperty: true);

            var k2 = customerType.GetOrAddKey(nameProperty);
            var k4 = customerType.GetOrAddKey(new[] { idProperty, otherNameProperty });
            var k3 = customerType.GetOrAddKey(new[] { idProperty, nameProperty });
            var k1 = customerType.GetOrAddKey(idProperty);

            Assert.True(new[] { k1, k2, k3, k4 }.SequenceEqual(customerType.Keys));
        }

        [Fact]
        public void Key_properties_are_always_read_only()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.False(((IProperty)idProperty).IsReadOnly);
            Assert.False(((IProperty)nameProperty).IsReadOnly);

            entityType.GetOrAddKey(new[] { idProperty, nameProperty });

            Assert.True(((IProperty)idProperty).IsReadOnly);
            Assert.True(((IProperty)nameProperty).IsReadOnly);

            nameProperty.IsReadOnly = true;

            Assert.Equal(
                Strings.KeyPropertyMustBeReadOnly(Customer.NameProperty.Name, typeof(Customer).FullName),
                Assert.Throws<NotSupportedException>(() => nameProperty.IsReadOnly = false).Message);

            Assert.True(((IProperty)idProperty).IsReadOnly);
            Assert.True(((IProperty)nameProperty).IsReadOnly);
        }

        [Fact]
        public void Can_add_a_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var customerKey = customerType.GetOrAddKey(idProperty);
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);

            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);

            Assert.NotNull(fk1);
            Assert.Same(fk1, orderType.GetForeignKey(customerFk1));
            Assert.Same(fk1, orderType.FindForeignKey(customerFk1));
            Assert.Same(fk1, orderType.GetOrAddForeignKey(customerFk1, new Key(new[] { idProperty })));
            Assert.Same(fk1, orderType.ForeignKeys.Single());

            var fk2 = orderType.AddForeignKey(customerFk2, customerKey);
            Assert.Same(fk2, orderType.GetForeignKey(customerFk2));
            Assert.Same(fk2, orderType.FindForeignKey(customerFk2));
            Assert.Same(fk2, orderType.GetOrAddForeignKey(customerFk2, new Key(new[] { idProperty })));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());
        }

        [Fact]
        public void Adding_a_foreign_key_throws_if_duplicate()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.AddForeignKey(customerFk1, customerKey);

            Assert.Equal(
                Strings.DuplicateForeignKey("{'" + Order.CustomerIdProperty.Name + "'}", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.AddForeignKey(customerFk1, customerKey)).Message);
        }

        [Fact]
        public void Adding_a_foreign_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.ForeignKeyPropertiesWrongEntity("{'" + Customer.IdProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.AddForeignKey(new[] { idProperty }, entityType2.GetOrAddKey(idProperty))).Message);
        }

        [Fact]
        public void Can_get_or_add_a_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var customerKey = customerType.GetOrAddKey(idProperty);
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);
            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);

            var fk2 = orderType.GetOrAddForeignKey(customerFk2, customerKey);

            Assert.NotNull(fk2);
            Assert.NotEqual(fk1, fk2);
            Assert.Same(fk2, orderType.GetForeignKey(customerFk2));
            Assert.Same(fk2, orderType.FindForeignKey(customerFk2));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());
            Assert.Same(fk2, orderType.GetOrAddForeignKey(customerFk2, customerKey));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_principal_type_name_plus_PK_name()
        {
            var fkProperty = DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());

            Assert.Same(fk, DependentType.FindForeignKey(
                PrincipalType,
                "SomeNav",
                "SomeInverse",
                null,
                null,
                isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_given_properties()
        {
            DependentType.GetOrAddProperty("SomeNavID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("SomeNavPeEKaY", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);
            var fkProperty = DependentType.GetOrAddProperty("HeToldMeYouKilledMyFk", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());

            Assert.Same(
                fk,
                DependentType.FindForeignKey(
                    PrincipalType,
                    "SomeNav",
                    "SomeInverse",
                    new[] { fkProperty },
                    new Property[0],
                    isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_given_property()
        {
            DependentType.GetOrAddProperty("SomeNavID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("SomeNavPeEKaY", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);
            var fkProperty1 = DependentType.GetOrAddProperty("No", typeof(int), shadowProperty: true);
            var fkProperty2 = DependentType.GetOrAddProperty("IAmYourFk", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(new[] { fkProperty1, fkProperty2 }, PrincipalType.GetOrAddKey(
                new[]
                    {
                        PrincipalType.GetOrAddProperty("Id1", typeof(int), shadowProperty: true),
                        PrincipalType.GetOrAddProperty("Id2", typeof(int), shadowProperty: true)
                    }));

            Assert.Same(
                fk,
                DependentType.FindForeignKey(
                    PrincipalType,
                    "SomeNav",
                    "SomeInverse",
                    new[] { fkProperty1, fkProperty2 },
                    new Property[0],
                    isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_navigation_plus_Id()
        {
            var fkProperty = DependentType.GetOrAddProperty("SomeNavID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("SomeNavPeEKaY", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());

            Assert.Same(
                fk,
                DependentType.FindForeignKey(
                    PrincipalType,
                    "SomeNav",
                    "SomeInverse",
                    null,
                    null,
                    isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_navigation_plus_PK_name()
        {
            var fkProperty = DependentType.GetOrAddProperty("SomeNavPeEKaY", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());

            Assert.Same(
                fk,
                DependentType.FindForeignKey(
                    PrincipalType,
                    "SomeNav",
                    "SomeInverse",
                    null,
                    null,
                    isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_finds_foreign_key_matching_principal_type_name_plus_Id()
        {
            var fkProperty = DependentType.GetOrAddProperty("PrincipalEntityID", typeof(int), shadowProperty: true);
            DependentType.GetOrAddProperty("PrincipalEntityPeEKaY", typeof(int), shadowProperty: true);

            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());

            Assert.Same(
                fk,
                DependentType.FindForeignKey(
                    PrincipalType,
                    "SomeNav",
                    "SomeInverse",
                    null,
                    null,
                    isUnique: false));
        }

        [Fact]
        public void TryGetForeignKey_does_not_find_existing_FK_if_FK_has_different_navigation_to_principal()
        {
            var fkProperty = DependentType.GetOrAddProperty("SharedFk", typeof(int), shadowProperty: true);
            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());
            DependentType.AddNavigation("AnotherNav", fk, pointsToPrincipal: true);

            var newFk = DependentType.FindForeignKey(
                PrincipalType,
                "SomeNav",
                "SomeInverse",
                new[] { fkProperty },
                new Property[0],
                isUnique: false);

            Assert.Null(newFk);
        }

        [Fact]
        public void TryGetForeignKey_does_not_find_existing_FK_if_FK_has_different_navigation_to_dependent()
        {
            var fkProperty = DependentType.GetOrAddProperty("SharedFk", typeof(int), shadowProperty: true);
            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());
            PrincipalType.AddNavigation("AnotherNav", fk, pointsToPrincipal: false);

            var newFk = DependentType.FindForeignKey(
                PrincipalType,
                "SomeNav",
                "SomeInverse",
                new[] { fkProperty },
                new Property[0],
                isUnique: false);

            Assert.Null(newFk);
        }

        [Fact]
        public void TryGetForeignKey_does_not_find_existing_FK_if_FK_has_different_uniqueness()
        {
            var fkProperty = DependentType.GetOrAddProperty("SharedFk", typeof(int), shadowProperty: true);
            var fk = DependentType.GetOrAddForeignKey(fkProperty, PrincipalType.GetPrimaryKey());
            fk.IsUnique = true;

            var newFk = DependentType.FindForeignKey(
                PrincipalType,
                "SomeNav",
                "SomeInverse",
                new[] { fkProperty },
                new Property[0],
                isUnique: false);

            Assert.Null(newFk);
        }

        private static Model BuildModel()
        {
            var model = new Model();

            var principalType = model.AddEntityType(typeof(PrincipalEntity));
            principalType.GetOrSetPrimaryKey(principalType.GetOrAddProperty("PeeKay", typeof(int)));

            var dependentType = model.AddEntityType(typeof(DependentEntity));
            dependentType.GetOrSetPrimaryKey(dependentType.GetOrAddProperty("KayPee", typeof(int), shadowProperty: true));

            return model;
        }

        private EntityType DependentType
        {
            get { return _model.GetEntityType(typeof(DependentEntity)); }
        }

        private EntityType PrincipalType
        {
            get { return _model.GetEntityType(typeof(PrincipalEntity)); }
        }

        private class PrincipalEntity
        {
            public int PeeKay { get; set; }
            public IEnumerable<DependentEntity> AnotherNav { get; set; }
        }

        private class DependentEntity
        {
            public PrincipalEntity Navigator { get; set; }
            public PrincipalEntity AnotherNav { get; set; }
        }

        [Fact]
        public void Can_remove_foreign_keys()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);

            Assert.Equal(
                Strings.ForeignKeyNotFound("{'" + Order.CustomerIdProperty.Name + "'}", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetForeignKey(customerFk1)).Message);
            Assert.Null(orderType.RemoveForeignKey(new ForeignKey(new[] { customerFk2 }, customerKey)));

            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);
            var fk2 = orderType.AddForeignKey(customerFk2, customerKey);

            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());

            Assert.Same(fk1, orderType.RemoveForeignKey(fk1));
            Assert.Null(orderType.RemoveForeignKey(fk1));

            Assert.Equal(
                Strings.ForeignKeyNotFound("{'" + Order.CustomerIdProperty.Name + "'}", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetForeignKey(customerFk1)).Message);
            Assert.Equal(new[] { fk2 }, orderType.ForeignKeys.ToArray());

            Assert.Same(fk2, orderType.RemoveForeignKey(new ForeignKey(new[] { customerFk2 }, customerKey)));

            Assert.Empty(orderType.ForeignKeys);
        }

        [Fact]
        public void Removing_a_foreign_key_throws_if_it_referenced_from_a_navigation_in_the_model()
        {
            var model = new Model();

            var customerType = model.AddEntityType(typeof(Customer));
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var fk = orderType.GetOrAddForeignKey(customerFk, customerKey);

            orderType.AddNavigation("Customer", fk, pointsToPrincipal: true);

            Assert.Equal(
                Strings.ForeignKeyInUse("{'" + Order.CustomerIdProperty.Name + "'}", typeof(Order).FullName, "Customer", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveForeignKey(fk)).Message);

            customerType.AddNavigation("Orders", fk, pointsToPrincipal: false);

            Assert.Equal(
                Strings.ForeignKeyInUse("{'" + Order.CustomerIdProperty.Name + "'}", typeof(Order).FullName, "Orders", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveForeignKey(fk)).Message);
        }

        [Fact]
        public void Foreign_keys_are_ordered_by_property_count_then_property_names()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = customerType.GetOrAddProperty(Customer.NameProperty);
            var customerKey = customerType.GetOrAddKey(idProperty);
            var otherCustomerKey = customerType.GetOrAddKey(new[] { idProperty, nameProperty });

            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);
            var customerFk3A = orderType.GetOrAddProperty("OtherId1", typeof(int), shadowProperty: true);
            var customerFk3B = orderType.GetOrAddProperty("OtherId2", typeof(string), shadowProperty: true);
            var customerFk4B = orderType.GetOrAddProperty("OtherId3", typeof(string), shadowProperty: true);

            var fk2 = orderType.AddForeignKey(customerFk2, customerKey);
            var fk4 = orderType.AddForeignKey(new[] { customerFk3A, customerFk4B }, otherCustomerKey);
            var fk3 = orderType.AddForeignKey(new[] { customerFk3A, customerFk3B }, otherCustomerKey);
            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);

            Assert.True(new[] { fk1, fk2, fk3, fk4 }.SequenceEqual(orderType.ForeignKeys));
        }

        [Fact]
        public void Can_add_and_remove_navigations()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);
            Assert.Null(orderType.RemoveNavigation(new Navigation("Customer", customerForeignKey, pointsToPrincipal: true)));

            var customerNavigation = orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);
            var ordersNavigation = customerType.AddNavigation("Orders", customerForeignKey, pointsToPrincipal: false);

            Assert.Equal("Customer", customerNavigation.Name);
            Assert.Same(orderType, customerNavigation.EntityType);
            Assert.Same(customerForeignKey, customerNavigation.ForeignKey);
            Assert.True(customerNavigation.PointsToPrincipal);
            Assert.False(customerNavigation.IsCollection());
            Assert.Same(customerType, customerNavigation.GetTargetType());
            Assert.Same(customerNavigation, customerForeignKey.GetNavigationToPrincipal());

            Assert.Equal("Orders", ordersNavigation.Name);
            Assert.Same(customerType, ordersNavigation.EntityType);
            Assert.Same(customerForeignKey, ordersNavigation.ForeignKey);
            Assert.False(ordersNavigation.PointsToPrincipal);
            Assert.True(ordersNavigation.IsCollection());
            Assert.Same(orderType, ordersNavigation.GetTargetType());
            Assert.Same(ordersNavigation, customerForeignKey.GetNavigationToDependent());

            Assert.Same(customerNavigation, orderType.Navigations.Single());
            Assert.Same(ordersNavigation, customerType.Navigations.Single());

            Assert.Same(customerNavigation, orderType.RemoveNavigation(customerNavigation));
            Assert.Null(orderType.RemoveNavigation(customerNavigation));
            Assert.Empty(orderType.Navigations);

            Assert.Same(ordersNavigation, customerType.RemoveNavigation(new Navigation("Orders", customerForeignKey, pointsToPrincipal: false)));
            Assert.Empty(customerType.Navigations);
        }

        [Fact]
        public void Can_add_new_navigations_or_get_existing_navigations()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            var customerNavigation = orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Equal("Customer", customerNavigation.Name);
            Assert.Same(orderType, customerNavigation.EntityType);
            Assert.Same(customerForeignKey, customerNavigation.ForeignKey);
            Assert.True(customerNavigation.PointsToPrincipal);
            Assert.False(customerNavigation.IsCollection());
            Assert.Same(customerType, customerNavigation.GetTargetType());

            Assert.Same(customerNavigation, orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: false));
            Assert.True(customerNavigation.PointsToPrincipal);
        }

        [Fact]
        public void Can_get_navigation_and_can_try_get_navigation()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            var customerNavigation = orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Same(customerNavigation, orderType.FindNavigation("Customer"));
            Assert.Same(customerNavigation, orderType.GetNavigation("Customer"));

            Assert.Null(orderType.FindNavigation("Nose"));

            Assert.Equal(
                Strings.NavigationNotFound("Nose", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetNavigation("Nose")).Message);
        }

        [Fact]
        public void Adding_a_new_navigation_with_a_name_that_already_exists_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Equal(
                Strings.DuplicateNavigation("Customer", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_that_belongs_to_a_different_type_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NavigationAlreadyOwned("Customer", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => customerType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_to_a_shadow_entity_type_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));

            var orderType = new EntityType("Order", new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty("CustomerId", typeof(int), shadowProperty: true);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NavigationOnShadowEntity("Customer", "Order"),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_pointing_to_a_shadow_entity_type_throws()
        {
            var customerType = new EntityType("Customer", new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty("CustomerId", typeof(int), shadowProperty: true);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NavigationToShadowEntity("Customer", typeof(Order).FullName, "Customer"),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_that_doesnt_match_a_CLR_property_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NoClrNavigation("Snook", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Snook", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Collection_navigation_properties_must_be_IEnumerables_of_the_target_type()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NavigationCollectionWrongClrType("NotCollectionOrders", typeof(Customer).FullName, typeof(Order).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("NotCollectionOrders", customerForeignKey, pointsToPrincipal: false)).Message);

            Assert.Equal(
                Strings.NavigationCollectionWrongClrType("DerivedOrders", typeof(Customer).FullName, typeof(IEnumerable<SpecialOrder>).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("DerivedOrders", customerForeignKey, pointsToPrincipal: false)).Message);
        }

        [Fact]
        public void Reference_navigation_properties_must_be_of_the_target_type()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.NavigationSingleWrongClrType("OrderCustomer", typeof(Order).FullName, typeof(Order).FullName, typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("OrderCustomer", customerForeignKey, pointsToPrincipal: true)).Message);

            Assert.Equal(
                Strings.NavigationSingleWrongClrType("DerivedCustomer", typeof(Order).FullName, typeof(SpecialCustomer).FullName, typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("DerivedCustomer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Multiple_sets_of_navigations_using_the_same_foreign_key_are_not_allowed()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            customerType.AddNavigation("EnumerableOrders", customerForeignKey, pointsToPrincipal: false);

            Assert.Equal(
                Strings.MultipleNavigations("Orders", "EnumerableOrders", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("Orders", customerForeignKey, pointsToPrincipal: false)).Message);
        }

        [Fact]
        public void Can_create_self_referencing_navigations()
        {
            var entityType = new EntityType(typeof(SelfRef), new Model());
            var fkProperty = entityType.AddProperty("ForeignKey", typeof(int));
            var principalEntityType = entityType;
            var principalKeyProperty = principalEntityType.AddProperty("Id", typeof(int));
            var referencedKey = principalEntityType.SetPrimaryKey(principalKeyProperty);
            var fk = entityType.AddForeignKey(fkProperty, referencedKey);
            fk.IsUnique = true;

            var navigationToDependent = principalEntityType.AddNavigation("SelfRef1", fk, pointsToPrincipal: false);
            var navigationToPrincipal = principalEntityType.AddNavigation("SelfRef2", fk, pointsToPrincipal: true);

            Assert.Same(fk.GetNavigationToDependent(), navigationToDependent);
            Assert.Same(fk.GetNavigationToPrincipal(), navigationToPrincipal);
        }

        [Fact]
        public void Navigations_are_ordered_by_name()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var customerForeignKeyProperty = orderType.AddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.AddForeignKey(customerForeignKeyProperty, customerKey);

            var specialOrderType = new EntityType(typeof(SpecialOrder), new Model());
            var specialCustomerForeignKeyProperty = specialOrderType.AddProperty(Order.CustomerIdProperty);
            var specialCustomerForeignKey = specialOrderType.AddForeignKey(specialCustomerForeignKeyProperty, customerKey);

            var navigation2 = customerType.AddNavigation("Orders", customerForeignKey, pointsToPrincipal: false);
            var navigation1 = customerType.AddNavigation("DerivedOrders", specialCustomerForeignKey, pointsToPrincipal: false);

            Assert.True(new[] { navigation1, navigation2 }.SequenceEqual(customerType.Navigations));
        }

        [Fact]
        public void Can_add_retrieve_and_remove_indexes()
        {
            var entityType = new EntityType(typeof(Order), new Model());
            var property1 = entityType.GetOrAddProperty(Order.IdProperty);
            var property2 = entityType.GetOrAddProperty(Order.CustomerIdProperty);

            Assert.Equal(0, entityType.Indexes.Count);
            Assert.Null(entityType.RemoveIndex(new Index(new[] { property1 })));

            var index1 = entityType.GetOrAddIndex(property1);

            Assert.Equal(1, index1.Properties.Count);
            Assert.Same(index1, entityType.GetIndex(property1));
            Assert.Same(index1, entityType.FindIndex(property1));
            Assert.Same(property1, index1.Properties[0]);

            var index2 = entityType.AddIndex(new[] { property1, property2 });

            Assert.Equal(2, index2.Properties.Count);
            Assert.Same(index2, entityType.GetOrAddIndex(new[] { property1, property2 }));
            Assert.Same(index2, entityType.FindIndex(new[] { property1, property2 }));
            Assert.Same(property1, index2.Properties[0]);
            Assert.Same(property2, index2.Properties[1]);

            Assert.Equal(2, entityType.Indexes.Count);
            Assert.Same(index1, entityType.Indexes[0]);
            Assert.Same(index2, entityType.Indexes[1]);

            Assert.Same(index1, entityType.RemoveIndex(index1));
            Assert.Null(entityType.RemoveIndex(index1));

            Assert.Equal(1, entityType.Indexes.Count);
            Assert.Same(index2, entityType.Indexes[0]);

            Assert.Same(index2, entityType.RemoveIndex(new Index(new[] { property1, property2 })));

            Assert.Equal(0, entityType.Indexes.Count);
        }

        [Fact]
        public void AddIndex_throws_if_not_from_same_entity()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var property1 = entityType1.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType1.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(Strings.IndexPropertiesWrongEntity("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Order).FullName),
                Assert.Throws<ArgumentException>(
                    () => entityType2.AddIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void AddIndex_throws_if_duplicate()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property1 = entityType.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.AddIndex(new[] { property1, property2 });

            Assert.Equal(Strings.DuplicateIndex("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => entityType.AddIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void GetIndex_throws_if_index_not_found()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property1 = entityType.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(Strings.IndexNotFound("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(
                    () => entityType.GetIndex(new[] { property1, property2 })).Message);

            entityType.AddIndex(property1);

            Assert.Equal(Strings.IndexNotFound("{'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'}", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(
                    () => entityType.GetIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void Can_add_and_remove_properties()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            Assert.Null(entityType.RemoveProperty(new Property("Id", typeof(int), entityType)));

            var property1 = entityType.AddProperty("Id", typeof(int));

            Assert.False(property1.IsShadowProperty);
            Assert.Equal("Id", property1.Name);
            Assert.Same(typeof(int), property1.ClrType);
            Assert.False(((IProperty)property1).IsConcurrencyToken);
            Assert.Same(entityType, property1.EntityType);

            var property2 = entityType.AddProperty("Name", typeof(string));

            Assert.True(new[] { property1, property2 }.SequenceEqual(entityType.Properties));

            Assert.Same(property1, entityType.RemoveProperty(property1));
            Assert.Null(entityType.RemoveProperty(property1));

            Assert.True(new[] { property2 }.SequenceEqual(entityType.Properties));

            Assert.Same(property2, entityType.RemoveProperty(new Property("Name", typeof(string), entityType)));

            Assert.Empty(entityType.Properties);
        }

        [Fact]
        public void Can_add_new_properties_or_get_existing_properties_using_PropertyInfo_or_name()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var idProperty = (IProperty)entityType.GetOrAddProperty("Id", typeof(int));

            Assert.False(idProperty.IsShadowProperty);
            Assert.Equal("Id", idProperty.Name);
            Assert.Same(typeof(int), idProperty.ClrType);
            Assert.False(idProperty.IsConcurrencyToken);
            Assert.Same(entityType, idProperty.EntityType);

            Assert.Same(idProperty, entityType.GetOrAddProperty(Customer.IdProperty));
            Assert.Same(idProperty, entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));
            Assert.False(idProperty.IsShadowProperty);

            var nameProperty = (IProperty)entityType.GetOrAddProperty("Name", typeof(string), shadowProperty: true);

            Assert.True(nameProperty.IsShadowProperty);
            Assert.Equal("Name", nameProperty.Name);
            Assert.Same(typeof(string), nameProperty.ClrType);
            Assert.False(nameProperty.IsConcurrencyToken);
            Assert.Same(entityType, nameProperty.EntityType);

            Assert.Same(nameProperty, entityType.GetOrAddProperty(Customer.NameProperty));
            Assert.Same(nameProperty, entityType.GetOrAddProperty("Name", typeof(string)));
            Assert.True(nameProperty.IsShadowProperty);

            Assert.True(new[] { idProperty, nameProperty }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrSetPrimaryKey(property);

            Assert.Equal(
                Strings.PropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_non_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrAddKey(property);

            Assert.Equal(
                Strings.PropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerPk = customerType.GetOrSetPrimaryKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerPk);

            Assert.Equal(
                Strings.PropertyInUse("CustomerId", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveProperty(customerFk)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_an_index()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrAddIndex(property);

            Assert.Equal(
                Strings.PropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Properties_are_ordered_by_name()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var property2 = entityType.AddProperty(Customer.NameProperty);
            var property1 = entityType.AddProperty(Customer.IdProperty);

            Assert.True(new[] { property1, property2 }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Primary_key_properties_precede_others()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var aProperty = entityType.AddProperty("A", typeof(int), true);
            var pkProperty = entityType.AddProperty(Customer.IdProperty);

            entityType.SetPrimaryKey(pkProperty);

            Assert.True(new[] { pkProperty, aProperty }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Composite_primary_key_properties_are_listed_in_key_order()
        {
            var entityType = new EntityType("CompositeKeyType", new Model());

            var aProperty = entityType.AddProperty("A", typeof(int), true);
            var pkProperty2 = entityType.AddProperty("aPK", typeof(int), true);
            var pkProperty1 = entityType.AddProperty("bPK", typeof(int), true);

            entityType.SetPrimaryKey(new[] { pkProperty1, pkProperty2 });

            Assert.True(new[] { pkProperty1, pkProperty2, aProperty }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Properties_on_base_type_are_listed_before_derived_properties()
        {
            var model = new Model();

            var parentType = new EntityType("Parent", model);
            var property2 = parentType.AddProperty("D", typeof(int), true);
            var property1 = parentType.AddProperty("C", typeof(int), true);

            var childType = new EntityType("Child", model);
            var property4 = childType.AddProperty("B", typeof(int), true);
            var property3 = childType.AddProperty("A", typeof(int), true);
            childType.BaseType = parentType;

            Assert.True(new[] { property1, property2, property3, property4 }.SequenceEqual(childType.Properties));
        }

        [Fact]
        public void Properties_are_properly_ordered_when_primary_key_changes()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var aProperty = entityType.AddProperty("A", typeof(int), true);
            var bProperty = entityType.AddProperty("B", typeof(int), true);

            entityType.SetPrimaryKey(bProperty);

            Assert.True(new[] { bProperty, aProperty }.SequenceEqual(entityType.Properties));

            entityType.SetPrimaryKey(aProperty);

            Assert.True(new[] { aProperty, bProperty }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Can_get_property_and_can_try_get_property()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            Assert.Same(property, entityType.FindProperty(Customer.IdProperty));
            Assert.Same(property, entityType.FindProperty("Id"));
            Assert.Same(property, entityType.GetProperty(Customer.IdProperty));
            Assert.Same(property, entityType.GetProperty("Id"));

            Assert.Null(entityType.FindProperty("Nose"));

            Assert.Equal(
                Strings.PropertyNotFound("Nose", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetProperty("Nose")).Message);
        }

        [Fact]
        public void Shadow_properties_have_CLR_flag_set_to_false()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddProperty("Id", typeof(int));
            entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);

            Assert.False(entityType.GetProperty("Name").IsShadowProperty);
            Assert.False(entityType.GetProperty("Id").IsShadowProperty);
            Assert.True(entityType.GetProperty("Mane").IsShadowProperty);
        }

        [Fact]
        public void Adding_a_new_property_with_a_name_that_already_exists_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.AddProperty("Id", typeof(int));

            Assert.Equal(
                Strings.DuplicateProperty("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Id", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_to_a_shadow_entity_type_throws()
        {
            var entityType = new EntityType("Hello", new Model());

            Assert.Equal(
                Strings.ClrPropertyOnShadowEntity("Kitty", "Hello"),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Kitty", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_that_doesnt_match_a_CLR_property_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(
                Strings.NoClrProperty("Snook", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Snook", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_where_the_type_doesnt_match_the_CLR_type_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(
                Strings.PropertyWrongClrType("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Id", typeof(string))).Message);
        }

        [Fact]
        public void Making_a_shadow_property_a_non_shadow_property_throws_if_CLR_property_does_not_match()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var property1 = entityType.AddProperty("Snook", typeof(int), shadowProperty: true);
            var property2 = entityType.AddProperty("Id", typeof(string), shadowProperty: true);

            Assert.Equal(
                Strings.NoClrProperty("Snook", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => property1.IsShadowProperty = false).Message);

            Assert.Equal(
                Strings.PropertyWrongClrType("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => property2.IsShadowProperty = false).Message);
        }

        [Fact]
        public void Can_get_property_indexes()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true);
            entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);

            Assert.Equal(0, entityType.GetProperty("Id").Index);
            Assert.Equal(1, entityType.GetProperty("Mane").Index);
            Assert.Equal(2, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Id").GetShadowIndex());
            Assert.Equal(1, entityType.GetProperty("Mane").GetShadowIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetShadowIndex());

            Assert.Equal(2, entityType.ShadowPropertyCount());
        }

        [Fact]
        public void Indexes_are_rebuilt_when_more_properties_added_or_relevant_state_changes()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());

            var nameProperty = entityType.GetOrAddProperty("Name", typeof(string));
            entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true).IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Id").Index);
            Assert.Equal(1, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Id").GetShadowIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetShadowIndex());

            Assert.Equal(0, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(1, entityType.ShadowPropertyCount());
            Assert.Equal(1, entityType.OriginalValueCount());

            var gameProperty = entityType.GetOrAddProperty("Game", typeof(int), shadowProperty: true);
            gameProperty.IsConcurrencyToken = true;

            var maneProperty = entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);
            maneProperty.IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Game").GetShadowIndex());
            Assert.Equal(1, entityType.GetProperty("Id").GetShadowIndex());
            Assert.Equal(2, entityType.GetProperty("Mane").GetShadowIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetShadowIndex());

            Assert.Equal(0, entityType.GetProperty("Game").GetOriginalValueIndex());
            Assert.Equal(1, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(2, entityType.GetProperty("Mane").GetOriginalValueIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(3, entityType.ShadowPropertyCount());
            Assert.Equal(3, entityType.OriginalValueCount());

            gameProperty.IsConcurrencyToken = false;
            nameProperty.IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Game").GetShadowIndex());
            Assert.Equal(1, entityType.GetProperty("Id").GetShadowIndex());
            Assert.Equal(2, entityType.GetProperty("Mane").GetShadowIndex());
            Assert.Equal(-1, entityType.GetProperty("Name").GetShadowIndex());

            Assert.Equal(-1, entityType.GetProperty("Game").GetOriginalValueIndex());
            Assert.Equal(0, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(1, entityType.GetProperty("Mane").GetOriginalValueIndex());
            Assert.Equal(2, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(3, entityType.ShadowPropertyCount());
            Assert.Equal(3, entityType.OriginalValueCount());

            gameProperty.IsShadowProperty = false;
            nameProperty.IsShadowProperty = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(-1, entityType.GetProperty("Game").GetShadowIndex());
            Assert.Equal(0, entityType.GetProperty("Id").GetShadowIndex());
            Assert.Equal(1, entityType.GetProperty("Mane").GetShadowIndex());
            Assert.Equal(2, entityType.GetProperty("Name").GetShadowIndex());

            Assert.Equal(-1, entityType.GetProperty("Game").GetOriginalValueIndex());
            Assert.Equal(0, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(1, entityType.GetProperty("Mane").GetOriginalValueIndex());
            Assert.Equal(2, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(3, entityType.ShadowPropertyCount());
            Assert.Equal(3, entityType.OriginalValueCount());
        }

        [Fact]
        public void Indexes_are_ordered_by_property_count_then_property_names()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = customerType.GetOrAddProperty(Customer.NameProperty);
            var otherProperty = customerType.GetOrAddProperty("OtherProperty", typeof(string), shadowProperty: true);

            var i2 = customerType.AddIndex(nameProperty);
            var i4 = customerType.AddIndex(new[] { idProperty, otherProperty });
            var i3 = customerType.AddIndex(new[] { idProperty, nameProperty });
            var i1 = customerType.AddIndex(idProperty);

            Assert.True(new[] { i1, i2, i3, i4 }.SequenceEqual(customerType.Indexes));
        }

        [Fact]
        public void Lazy_original_values_are_used_for_full_notification_and_shadow_enties()
        {
            Assert.False(new EntityType(typeof(FullNotificationEntity), new Model()).UseEagerSnapshots);
        }

        [Fact]
        public void Lazy_original_values_are_used_for_shadow_enties()
        {
            Assert.False(new EntityType("Z'ha'dum", new Model()).UseEagerSnapshots);
        }

        [Fact]
        public void Eager_original_values_are_used_for_enties_that_only_implement_INotifyPropertyChanged()
        {
            Assert.True(new EntityType(typeof(ChangedOnlyEntity), new Model()).UseEagerSnapshots);
        }

        [Fact]
        public void Eager_original_values_are_used_for_enties_that_do_no_notification()
        {
            Assert.True(new EntityType(typeof(Customer), new Model()).UseEagerSnapshots);
        }

        [Fact]
        public void Lazy_original_values_can_be_switched_off()
        {
            Assert.False(new EntityType(typeof(FullNotificationEntity), new Model()) { UseEagerSnapshots = false }.UseEagerSnapshots);
        }

        [Fact]
        public void Lazy_original_values_can_be_switched_on_but_only_if_entity_does_not_require_eager_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model()) { UseEagerSnapshots = true };
            entityType.UseEagerSnapshots = false;
            Assert.False(entityType.UseEagerSnapshots);

            Assert.Equal(
                Strings.EagerOriginalValuesRequired(typeof(ChangedOnlyEntity).FullName),
                Assert.Throws<InvalidOperationException>(() => new EntityType(typeof(ChangedOnlyEntity), new Model()) { UseEagerSnapshots = false }).Message);
        }

        [Fact]
        public void All_properties_have_original_value_indexes_when_using_eager_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model()) { UseEagerSnapshots = true };

            entityType.GetOrAddProperty("Name", typeof(string));
            entityType.GetOrAddProperty("Id", typeof(int));

            Assert.Equal(0, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(1, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(2, entityType.OriginalValueCount());
        }

        [Fact]
        public void Only_required_properties_have_original_value_indexes_when_using_lazy_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());

            entityType.GetOrAddProperty("Name", typeof(string)).IsConcurrencyToken = true;
            entityType.GetOrAddProperty("Id", typeof(int));

            Assert.Equal(-1, entityType.GetProperty("Id").GetOriginalValueIndex());
            Assert.Equal(0, entityType.GetProperty("Name").GetOriginalValueIndex());

            Assert.Equal(1, entityType.OriginalValueCount());
        }

        [Fact]
        public void FK_properties_are_marked_as_requiring_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());
            entityType.GetOrSetPrimaryKey(entityType.GetOrAddProperty("Id", typeof(int)));

            Assert.Equal(-1, entityType.GetProperty("Id").GetOriginalValueIndex());

            entityType.GetOrAddForeignKey(new[] { entityType.GetOrAddProperty("Id", typeof(int)) }, entityType.GetPrimaryKey());

            Assert.Equal(0, entityType.GetProperty("Id").GetOriginalValueIndex());
        }

        private class Customer
        {
            public static readonly PropertyInfo IdProperty = typeof(Customer).GetProperty("Id");
            public static readonly PropertyInfo NameProperty = typeof(Customer).GetProperty("Name");

            public int Id { get; set; }
            public Guid Unique { get; set; }
            public string Name { get; set; }
            public string Mane { get; set; }
            public ICollection<Order> Orders { get; set; }

            public IEnumerable<Order> EnumerableOrders { get; set; }
            public Order NotCollectionOrders { get; set; }
            public IEnumerable<SpecialOrder> DerivedOrders { get; set; }
        }

        private class SpecialCustomer : Customer
        {
        }

        private class Order
        {
            public static readonly PropertyInfo IdProperty = typeof(Order).GetProperty("Id");
            public static readonly PropertyInfo CustomerIdProperty = typeof(Order).GetProperty("CustomerId");
            public static readonly PropertyInfo CustomerUniqueProperty = typeof(Order).GetProperty("CustomerUnique");

            public int Id { get; set; }
            public int CustomerId { get; set; }
            public Guid CustomerUnique { get; set; }
            public Customer Customer { get; set; }

            public Order OrderCustomer { get; set; }
            public SpecialCustomer DerivedCustomer { get; set; }
        }

        private class SpecialOrder : Order
        {
        }

        private class FullNotificationEntity : INotifyPropertyChanging, INotifyPropertyChanged
        {
            private int _id;
            private string _name;
            private int _game;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        NotifyChanging();
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        NotifyChanging();
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public int Game
            {
                get { return _game; }
                set
                {
                    if (_game != value)
                    {
                        NotifyChanging();
                        _game = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangingEventHandler PropertyChanging;
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            private void NotifyChanging([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
            }
        }

        private class ChangedOnlyEntity : INotifyPropertyChanged
        {
            private int _id;
            private string _name;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private class SelfRef
        {
            public int Id { get; set; }
            public SelfRef SelfRef1 { get; set; }
            public SelfRef SelfRef2 { get; set; }
            public int ForeignKey { get; set; }
        }
    }
}
