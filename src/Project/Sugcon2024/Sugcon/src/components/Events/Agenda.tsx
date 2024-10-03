import React from 'react';
import InnerHTML from 'dangerously-set-html-content';
import {
  Field,
  GetServerSideComponentProps, GetStaticComponentProps,
  useComponentProps,
  withDatasourceCheck
} from '@sitecore-jss/sitecore-jss-nextjs';
import useSWR, {SWRConfig} from 'swr';
import { ComponentProps } from 'lib/component-props';

interface Fields {
  SessionizeUrl: Field<string>;
}

type AgendaProps = ComponentProps & {
  fields: Fields;
};

const fetcher = (url: string) => fetch(url).then((res) => res.text());

const AgendaDefaultComponent = (props: AgendaProps): JSX.Element => (
    <div className={`component promo ${props.params.styles}`}>
      <div className="component-content">
        <span className="is-empty-hint">Agenda</span>
      </div>
    </div>
);

const AgendaComponent = (props: AgendaProps): JSX.Element => {
  const id = props.params.RenderingIdentifier;

  console.log('props.rendering.uid', props.rendering.uid);
  
  const componentProps = useComponentProps(props.rendering.uid) || {};

  // console.dir(componentProps, { depth: null });

  const api = props.fields.SessionizeUrl.value;
  const { data, error } = useSWR(
      api,
      fetcher
  );

  if (!props?.fields?.SessionizeUrl?.value) {
    return <AgendaDefaultComponent {...props} />;
  }

  //TODO: design error
  if (error) {
    return <div>Failed to load...</div>;
  }

  //TODO: design loading
  if (!data) {
    return <div>Loading</div>;
  }

  return (
      <div className={`component agenda ${props.params.styles}`} id={id ? id : undefined}>
        <div className="component-content">
          <InnerHTML html={data} />
        </div>
      </div>
  );
};

const AgendaWithSWRConfig = (props: AgendaProps & { fallback: any }) => {
  const componentProps = useComponentProps(props.rendering.uid) || {};
  //console.log('componentProps', componentProps);
  //console.log('fallback', props.fallback);
  return (
      <SWRConfig value={{ fallback: componentProps.props.fallback }}>
        <AgendaComponent {...props} />
      </SWRConfig>
  );
};
export const getServerSideProps: GetServerSideComponentProps = async (rendering, layoutData, context) => {
  // console.log('getServerSideProps');
  // console.dir(layoutData, { depth: null });
  // console.dir(rendering?.fields?.SessionizeUrl?.value, { depth: null });
  // console.log(context, context);

  const api = rendering?.fields?.SessionizeUrl?.value;
  const data = await fetcher(api);
  
  // console.log('data', data);

  return {
    props: {
      fallback: {
        [api]: data
      }
    }
  };
};

export const getStaticProps: GetStaticComponentProps = async (_, layoutData, context) => {
  console.log('getStaticProps');
  // console.log(layoutData, layoutData);
  // console.log(context, context);
  return {
    assetDetailsStatic: 'Agenda test',
  };
};

export const Default = withDatasourceCheck()<AgendaProps>(AgendaWithSWRConfig);

