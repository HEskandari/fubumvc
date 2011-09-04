using System;
using FubuMVC.Core.Assets;
using FubuMVC.Core.Assets.Combination;
using FubuMVC.Core.Assets.Tags;
using FubuCore;
using System.Collections.Generic;
using System.Linq;

namespace FubuMVC.Core.Registration.DSL
{
    public class AssetsExpression
    {
        private readonly FubuRegistry _registry;
        private readonly Lazy<IAssetRegistration> _registration;

        public AssetsExpression(FubuRegistry registry)
        {
            _registry = registry;

            // TODO -- end to end tests here
            _registration = new Lazy<IAssetRegistration>(() =>
            {
                var recording = new RecordingAssetRegistration();
                _registry.Services(x => x.AddService<IAssetPolicy>(recording));

                return recording;
            });
        }

        public AssetsExpression(FubuRegistry registry, IAssetRegistration registration)
        {
            _registry = registry;
            _registration = new Lazy<IAssetRegistration>(() => registration);
        }

        /// <summary>
        /// If applyPolicy is true, this directs FubuMVC to throw up the
        /// Yellow Screen of Death to fail fast if unknown or missing
        /// assets are requested in any screen.  This mode is mostly
        /// appropriate for *development mode*
        /// </summary>
        /// <param name="applyPolicy"></param>
        /// <returns></returns>
        public AssetsExpression YSOD_on_missing_assets(bool applyPolicy)
        {
            if (applyPolicy)
            {
                setService<IMissingAssetHandler, YellowScreenMissingAssetHandler>();
            }

            return this;
        }

        /// <summary>
        /// Replace the IMissingAssetHandler with a custom implementation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetsExpression HandleMissingAssetsWith<T>() where T : IMissingAssetHandler
        {
            setService<IMissingAssetHandler, T>();

            return this;
        }

        /// <summary>
        /// This directs FubuMVC to apply a *very* naive combination policy to create a combination
        /// for each unique set of requested assets (styles will only be combined if they are in the
        /// same folder).  
        /// </summary>
        /// <returns></returns>
        public AssetsExpression CombineAllUniqueAssetRequests()
        {
            setService<ICombinationDeterminationService, CombineAllUniqueSetsCombinationDeterminationService>();
            return this;
        }

        /// <summary>
        /// Additively registers an ICombinationPolicy.  NOTE:  this will have no
        /// effect if you are using the CombineAllUniqueAssetRequests() options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetsExpression CombineWith<T>() where T : ICombinationPolicy
        {
            addService<ICombinationPolicy, T>();
            return this;
        }



        private void addService<TInterface, TConcrete>() where TConcrete : TInterface
        {
            _registry.Services(x => x.AddService<TInterface, TConcrete>());
        }

        private void setService<TInterface, TConcrete>() where TConcrete : TInterface
        {
            _registry.Services(x => x.ReplaceService<TInterface, TConcrete>());
        }

        public AssetFileExpression Asset(string assetName)
        {
            return new AssetFileExpression(this, assetName);
        }

        public class AssetFileExpression
        {
            private readonly AssetsExpression _parent;
            private readonly string _assetName;
            private IAssetRegistration _registration;

            public AssetFileExpression(AssetsExpression parent, string assetName)
            {
                _parent = parent;
                _assetName = assetName;
                _registration = _parent._registration.Value;
            }

            public AssetsExpression Preceeds(string afterName)
            {
                _registration.Preceeding(_assetName, afterName);
                return _parent;
            }

            public AssetsExpression Extends(string @base)
            {
                _registration.Extension(_assetName, @base);
                return _parent;
            }

            public AssetsExpression Requires(string commaDelimitedAssetNames)
            {
                commaDelimitedAssetNames.ToDelimitedArray().Each(x => _registration.Dependency(_assetName, x));
                return _parent;
            }
        }


        public AliasExpression Alias(string aliasName)
        {
            return new AliasExpression(this, aliasName);
        }

        public class AliasExpression
        {
            private readonly AssetsExpression _parent;
            private readonly string _aliasName;

            public AliasExpression(AssetsExpression parent, string aliasName)
            {
                _parent = parent;
                _aliasName = aliasName;
            }

            public AssetsExpression Is(string assetName)
            {
                _parent._registration.Value.Alias(assetName, _aliasName);
                return _parent;
            }
        }

        public AssetSetExpression AssetSet(string setName)
        {
            return new AssetSetExpression(this, setName);
        }

        public class AssetSetExpression
        {
            private readonly AssetsExpression _parent;
            private readonly string _setName;

            public AssetSetExpression(AssetsExpression parent, string setName)
            {
                _parent = parent;
                _setName = setName;
            }

            public AssetsExpression Includes(string commaDelimitedAssetNames)
            {
                commaDelimitedAssetNames.ToDelimitedArray().Each(x => _parent._registration.Value.AddToSet(_setName, x));
                return _parent;
            }
        }

        public OrderedSetExpression OrderedSet(string setName)
        {
            return new OrderedSetExpression(this, setName);
        }

        public class OrderedSetExpression
        {
            private readonly AssetsExpression _parent;
            private readonly string _setName;

            public OrderedSetExpression(AssetsExpression parent, string setName)
            {
                _parent = parent;
                _setName = setName;
            }

            public AssetsExpression Is(string commaDelimitedAssetNames)
            {
                var names = commaDelimitedAssetNames.ToDelimitedArray();
                var registration = _parent._registration.Value;
                names.Each(x => registration.AddToSet(_setName, x));

                var queue = new Queue<string>(names.Reverse());
                while (queue.Count > 1)
                {
                    var name = queue.Dequeue();
                    registration.Dependency(name, queue.Peek());
                }

                return _parent;
            }
        }
    }
}