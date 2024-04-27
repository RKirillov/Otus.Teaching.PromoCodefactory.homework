using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Dsl;
using AutoFixture.Xunit2;
using Castle.Components.DictionaryAdapter.Xml;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Namotion.Reflection;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class CancelPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly SetPartnerPromoCodeLimitRequest _partnerLimitRequest;
        //private readonly Mock<SetPartnerPromoCodeLimitRequest> _setPartnerPromoCodeLimitRequest;

        public CancelPartnerPromoCodeLimitAsyncTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();

            //var someEntity = new Fixture().Build<Entity>().With(e => e.Name, "Important For Test").Without(e => e.Group).Create();
            _partnerLimitRequest = fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(f => f.Limit, 10)
                .With(f => f.EndDate, DateTime.Now).Create();
        }

        public Partner CreateBasePartner(string id = "def47943-7aaf-44a1-ae21-05aa4948b165")
        {
            var partner = new Partner()
            {
                Id = Guid.Parse(id),
                Name = "Суперигрушки",
                IsActive = true,
                PartnerLimits = new List<PartnerPromoCodeLimit>()
                {
                    new PartnerPromoCodeLimit()
                    {
                        Id = Guid.Parse("e00633a5-978a-420e-a7d6-3e1dab116393"),
                        CreateDate = new DateTime(2020, 07, 9),
                        EndDate = new DateTime(2020, 10, 9),
                        Limit = 100
                    }
                }
            };

            return partner;
        }

        [Fact]
        public async void CancelPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.CancelPartnerPromoCodeLimitAsync(partnerId);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Fact]
        public async void CancelPartnerPromoCodeLimitAsync_PartnerIsNotActive_ReturnsBadRequest()
        {
            // Arrange без разницы какой id устанавливать
            var partnerId = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8");
            //var partnerId=Guid.Empty;
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partner.IsActive = false;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.CancelPartnerPromoCodeLimitAsync(partnerId);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }
        //закончили CancelPartnerPromoCodeLimitAsync
        //тестируем SetPartnerPromoCodeLimitAsync 

        //Также есть функционал работы с партнерам, для партнера устанавливаются лимиты на выдачу промокодов, если лимит превышен или закончился срок его действия, то промокод нельзя выдать
        //Если партнер не найден, то также нужно выдать ошибку 404;
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;
            var partnerLimitRequest = new SetPartnerPromoCodeLimitRequest();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, partnerLimitRequest);


            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        //
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActive_ReturnsBadRequest(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            //var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            var partner = CreateBasePartner("def47943-7aaf-44a1-ae21-05aa4948b165");
            partner.IsActive = false;

            //спрятать в autofixture
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        //Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes,
        //[Fact]
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_ResetPromocodes_LimitsOver(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partner.NumberIssuedPromoCodes = 100;
            //спрятать в autofixture
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = (await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest));

            var zz = (result as CreatedAtActionResult).Value;
            var zx = result.As<CreatedAtActionResult>();
            var z= result.As<Partner>();
            // Assert
            //result.PartnerLimits.Select(x => x.Partner.NumberIssuedPromoCodes).First().Should().Be(0);
        }
        // если лимит закончился, то количество не обнуляется;
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_ResetPromocodes_LimitsGood()
        {
        }


        //При установке лимита нужно отключить предыдущий лимит;

        //Лимит должен быть больше 0;

        //Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом);
        //Если в текущей реализации найдутся ошибки, то их нужно исправить и желательно написать тест, чтобы они больше не повторялись.
    }
}